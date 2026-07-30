using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modulo_Tesoreria_ValidarPagosDirectos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. SP: Obtener la lista de cobros de una Cuota específica para la Matriz de Pagos del Aula
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_ObtenerDetalleCobroEstudiantes
                    @CuotaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        cd.Id AS CuotaDetalleId,
                        cd.CuotaId,
                        cd.EstudianteId,
                        e.Nombres + ' ' + e.ApellidoPaterno + ' ' + e.ApellidoMaterno AS EstudianteNombreCompleto,
                        e.TipoDocumento + ': ' + e.NumeroDocumento AS EstudianteDocumento,
                        e.NombreApoderado,
                        e.TelefonoApoderado,
                        cd.MontoAsignado,
                        cd.MontoPagado,
                        cd.EstadoPago, -- 'PENDIENTE', 'PARCIAL', 'COMPLETO'
                        cd.FechaUltimoPago
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Estudiantes e ON cd.EstudianteId = e.Id
                    WHERE cd.CuotaId = @CuotaId
                    ORDER BY e.ApellidoPaterno ASC, e.ApellidoMaterno ASC, e.Nombres ASC;
                END
            ");

            // 2. SP: Registrar o Actualizar el Pago Manual de un Estudiante por la Tesorera
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_RegistrarPagoManual
                    @CuotaDetalleId INT,
                    @MontoAbonado DECIMAL(10,2),
                    @FormaPago VARCHAR(30) = 'YAPE' -- 'YAPE', 'PLIN', 'EFECTIVO', 'TRANSFERENCIA'
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;
                    BEGIN TRY

                        UPDATE CuotaDetalleEstudiante
                        SET MontoPagado = MontoPagado + @MontoAbonado,
                            EstadoPago = CASE 
                                WHEN (MontoPagado + @MontoAbonado) >= MontoAsignado THEN 'COMPLETO'
                                ELSE 'PARCIAL'
                            END,
                            FechaUltimoPago = DATEADD(HOUR, -5, GETUTCDATE())
                        WHERE Id = @CuotaDetalleId;

                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH;
                END
            ");

            // 3. SP: Revertir/Anular Pago en caso de error de la tesorera
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_AnularPagoEstudiante
                    @CuotaDetalleId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    UPDATE CuotaDetalleEstudiante
                    SET MontoPagado = 0.00,
                        EstadoPago = 'PENDIENTE',
                        FechaUltimoPago = NULL
                    WHERE Id = @CuotaDetalleId;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Cuotas_AnularPagoEstudiante");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Cuotas_RegistrarPagoManual");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Cuotas_ObtenerDetalleCobroEstudiantes");
        }
    }
}
