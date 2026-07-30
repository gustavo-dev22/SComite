using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Actualizar_Ordenamiento_Y_Reajuste_Cuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Actualizar sp_Cuotas_ObtenerPorAula para ordenar cronológicamente e incluir tipo de cuota
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_ObtenerPorAula
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        c.Id,
                        c.AulaId,
                        c.Concepto,
                        c.MontoIndividual,
                        c.FechaVencimiento,
                        c.Estado,
                        c.Observacion,
                        c.FechaCreacion,
                        ISNULL(c.TipoCuota, 'EXTRAORDINARIA') AS TipoCuota,
                        c.MesCorrespondiente,
                        COUNT(cd.Id) AS TotalEstudiantesAsignados,
                        ISNULL(SUM(cd.MontoAsignado), 0.00) AS TotalMontoEsperado,
                        ISNULL(SUM(cd.MontoPagado), 0.00) AS TotalMontoRecaudado,
                        ISNULL(SUM(CASE WHEN cd.EstadoPago = 'COMPLETO' THEN 1 ELSE 0 END), 0) AS EstudiantesAlDia,
                        ISNULL(SUM(CASE WHEN cd.EstadoPago = 'PENDIENTE' THEN 1 ELSE 0 END), 0) AS EstudiantesPendientes
                    FROM Cuotas c
                    LEFT JOIN CuotaDetalleEstudiante cd ON c.Id = cd.CuotaId
                    WHERE c.AulaId = @AulaId
                    GROUP BY c.Id, c.AulaId, c.Concepto, c.MontoIndividual, c.FechaVencimiento, c.Estado, c.Observacion, c.FechaCreacion, c.TipoCuota, c.MesCorrespondiente
                    ORDER BY c.FechaVencimiento ASC; -- 🚀 Orden cronológico de vencimiento
                END
            ");

            // 2. Actualizar sp_Cuotas_GenerarProgramacionMensual para soportar @MesInicio y Reajuste de Montos
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_GenerarProgramacionMensual
                    @AulaId INT,
                    @ConceptoBase VARCHAR(100),
                    @MontoMensual DECIMAL(10,2),
                    @MesInicio INT = 3, -- 3=Marzo, 4=Abril, etc.
                    @DiaVencimiento INT = 10,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @Mes INT = @MesInicio;
                    DECLARE @FechaVencimiento DATE;
                    DECLARE @ConceptoMes VARCHAR(150);
                    DECLARE @NuevaCuotaId INT;

                    WHILE @Mes <= 12
                    BEGIN
                        -- Fecha de vencimiento exacta
                        SET @FechaVencimiento = DATEFROMPARTS(@AnioLectivo, @Mes, @DiaVencimiento);

                        -- Nombre del mes en español
                        DECLARE @NombreMes VARCHAR(20) = CASE @Mes
                            WHEN 3 THEN 'Marzo' WHEN 4 THEN 'Abril' WHEN 5 THEN 'Mayo'
                            WHEN 6 THEN 'Junio' WHEN 7 THEN 'Julio' WHEN 8 THEN 'Agosto'
                            WHEN 9 THEN 'Setiembre' WHEN 10 THEN 'Octubre' WHEN 11 THEN 'Noviembre'
                            WHEN 12 THEN 'Diciembre'
                        END;

                        SET @ConceptoMes = @ConceptoBase + ' - ' + @NombreMes + ' ' + CAST(@AnioLectivo AS VARCHAR(4));

                        -- 🚀 CASUÍSTICA 1: Si la cuota ya existe para ese mes, se reajusta/actualiza
                        IF EXISTS (SELECT 1 FROM Cuotas WHERE AulaId = @AulaId AND TipoCuota = 'RECURRENTE_MENSUAL' AND MesCorrespondiente = @Mes)
                        BEGIN
                            UPDATE Cuotas 
                            SET MontoIndividual = @MontoMensual,
                                FechaVencimiento = @FechaVencimiento
                            WHERE AulaId = @AulaId AND TipoCuota = 'RECURRENTE_MENSUAL' AND MesCorrespondiente = @Mes;

                            -- Actualizar el monto asignado en la cobranza de estudiantes con pago PENDIENTE
                            UPDATE cd
                            SET cd.MontoAsignado = @MontoMensual
                            FROM CuotaDetalleEstudiante cd
                            INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                            WHERE c.AulaId = @AulaId 
                              AND c.TipoCuota = 'RECURRENTE_MENSUAL' 
                              AND c.MesCorrespondiente = @Mes
                              AND cd.EstadoPago = 'PENDIENTE';
                        END
                        ELSE
                        -- 🚀 CASUÍSTICA 2: Si no existe, se inserta la nueva cuota mensual
                        BEGIN
                            INSERT INTO Cuotas (AulaId, Concepto, MontoIndividual, FechaVencimiento, Estado, Observacion, TipoCuota, MesCorrespondiente)
                            VALUES (@AulaId, @ConceptoMes, @MontoMensual, @FechaVencimiento, 'EN COBRO', 'Cuota mensual programada de caja chica', 'RECURRENTE_MENSUAL', @Mes);

                            SET @NuevaCuotaId = SCOPE_IDENTITY();

                            -- Asignar la obligación de pago a todos los estudiantes activos del aula
                            INSERT INTO CuotaDetalleEstudiante (CuotaId, EstudianteId, MontoAsignado, MontoPagado, EstadoPago)
                            SELECT @NuevaCuotaId, Id, @MontoMensual, 0.00, 'PENDIENTE'
                            FROM Estudiantes
                            WHERE AulaId = @AulaId AND Estado = 1;
                        END

                        SET @Mes = @Mes + 1;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_ObtenerPorAula
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT c.*, COUNT(cd.Id) AS TotalEstudiantesAsignados, 
                           SUM(cd.MontoAsignado) AS TotalMontoEsperado, 
                           SUM(cd.MontoPagado) AS TotalMontoRecaudado,
                           SUM(CASE WHEN cd.EstadoPago = 'COMPLETO' THEN 1 ELSE 0 END) AS EstudiantesAlDia,
                           SUM(CASE WHEN cd.EstadoPago = 'PENDIENTE' THEN 1 ELSE 0 END) AS EstudiantesPendientes
                    FROM Cuotas c
                    LEFT JOIN CuotaDetalleEstudiante cd ON c.Id = cd.CuotaId
                    WHERE c.AulaId = @AulaId
                    GROUP BY c.Id, c.AulaId, c.Concepto, c.MontoIndividual, c.FechaVencimiento, c.Estado, c.Observacion, c.FechaCreacion, c.TipoCuota, c.MesCorrespondiente
                    ORDER BY c.FechaCreacion DESC;
                END
            ");
        }
    }
}
