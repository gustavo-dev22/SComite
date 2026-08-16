using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixEstadoPagoCompletoEnSPs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🛡️ P2: El estado canónico de pago cuando el estudiante cancela todo es 'COMPLETO'
            // (PENDIENTE / PARCIAL / COMPLETO). Los SPs de Registro/Anulación escribían 'PAGADO',
            // lo que rompía los filtros y reportes que buscan 'COMPLETO'. Se re-crean con la
            // misma lógica de integridad (TOCTOU, anulación no destructiva) pero con 'COMPLETO'.

            // 1. SP: Registrar Pago Manual (control TOCTOU + estado 'COMPLETO')
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_RegistrarPagoManual
                    @CuotaDetalleId INT,
                    @MontoAbonado DECIMAL(10,2),
                    @FormaPago VARCHAR(30) = 'YAPE', -- 'YAPE', 'PLIN', 'EFECTIVO', 'TRANSFERENCIA'
                    @ComprobanteReferencia NVARCHAR(100) = NULL,
                    @UsuarioRegistro NVARCHAR(100) = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;
                    BEGIN TRY

                        DECLARE @EstadoCuota VARCHAR(20);
                        DECLARE @DeudaPendiente DECIMAL(10,2);

                        -- 1. Leer estado de la cuota y deuda pendiente DENTRO de la transacción (protección TOCTOU)
                        SELECT @EstadoCuota = c.Estado,
                               @DeudaPendiente = cd.MontoAsignado - cd.MontoPagado
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON c.Id = cd.CuotaId
                        WHERE cd.Id = @CuotaDetalleId;

                        IF @EstadoCuota IS NULL
                            THROW 50003, 'El detalle de cuota no existe.', 1;

                        IF UPPER(@EstadoCuota) = 'CERRADA'
                            THROW 50002, 'No se pueden registrar pagos en una cuota cerrada.', 1;

                        IF @MontoAbonado > @DeudaPendiente
                            THROW 50001, 'El monto abonado excede la deuda pendiente.', 1;

                        -- 2. Aplicar el abono al detalle del estudiante
                        UPDATE CuotaDetalleEstudiante
                        SET MontoPagado = MontoPagado + @MontoAbonado,
                            EstadoPago = CASE
                                WHEN (MontoPagado + @MontoAbonado) >= MontoAsignado THEN 'COMPLETO'
                                ELSE 'PARCIAL'
                            END,
                            FechaUltimoPago = DATEADD(HOUR, -5, GETUTCDATE())
                        WHERE Id = @CuotaDetalleId;

                        -- 3. Registrar el abono individual en el historial (auditoría no destructiva)
                        INSERT INTO CuotaPagosHistorial (CuotaDetalleId, Monto, FormaPago, ComprobanteReferencia, FechaPago, UsuarioRegistro, Estado)
                        VALUES (@CuotaDetalleId, @MontoAbonado, @FormaPago, @ComprobanteReferencia, DATEADD(HOUR, -5, GETUTCDATE()), @UsuarioRegistro, 'ACTIVO');

                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH;
                END
            ");

            // 2. SP: Anular Pago de forma NO destructiva (anula el último abono activo y recalcula saldo)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_AnularPagoEstudiante
                    @CuotaDetalleId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;
                    BEGIN TRY

                        DECLARE @EstadoCuota VARCHAR(20);
                        DECLARE @UltimoAbonoId INT = NULL;
                        DECLARE @MontoUltimoAbono DECIMAL(10,2) = 0.00;

                        -- 1. Validar que la cuota no esté CERRADA (dentro de la transacción)
                        SELECT @EstadoCuota = c.Estado
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON c.Id = cd.CuotaId
                        WHERE cd.Id = @CuotaDetalleId;

                        IF UPPER(ISNULL(@EstadoCuota, '')) = 'CERRADA'
                            THROW 50012, 'No se pueden anular pagos en una cuota cerrada.', 1;

                        -- 2. Obtener el último abono ACTIVO del historial
                        SELECT TOP 1 @UltimoAbonoId = Id, @MontoUltimoAbono = Monto
                        FROM CuotaPagosHistorial
                        WHERE CuotaDetalleId = @CuotaDetalleId AND Estado = 'ACTIVO'
                        ORDER BY Id DESC;

                        IF @UltimoAbonoId IS NULL
                            THROW 50011, 'No existe un pago activo para anular en este detalle de cuota.', 1;

                        -- 3. Marcar el abono como ANULADO (anulación no destructiva)
                        UPDATE CuotaPagosHistorial
                        SET Estado = 'ANULADO'
                        WHERE Id = @UltimoAbonoId;

                        -- 4. Restar únicamente el monto del abono anulado y recalcular el estado
                        UPDATE CuotaDetalleEstudiante
                        SET MontoPagado = MontoPagado - @MontoUltimoAbono,
                            EstadoPago = CASE
                                WHEN (MontoPagado - @MontoUltimoAbono) <= 0 THEN 'PENDIENTE'
                                WHEN (MontoPagado - @MontoUltimoAbono) >= MontoAsignado THEN 'COMPLETO'
                                ELSE 'PARCIAL'
                            END,
                            FechaUltimoPago = CASE
                                WHEN (MontoPagado - @MontoUltimoAbono) <= 0 THEN NULL
                                ELSE FechaUltimoPago
                            END
                        WHERE Id = @CuotaDetalleId;

                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Los SPs anteriores son equivalentes en comportamiento salvo por el estado 'PAGADO'
            // vs 'COMPLETO'; la reversión no requiere operaciones destructivas.
        }
    }
}
