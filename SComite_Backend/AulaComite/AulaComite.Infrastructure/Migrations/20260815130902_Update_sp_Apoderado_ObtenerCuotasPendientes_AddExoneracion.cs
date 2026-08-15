using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_sp_Apoderado_ObtenerCuotasPendientes_AddExoneracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Apoderado_ObtenerCuotasPendientes]
                    @EstudianteId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AulaId INT;

                    SELECT TOP 1 @AulaId = a.Id
                    FROM Estudiantes e
                    JOIN Aulas a ON a.Id = e.AulaId
                    JOIN PeriodosLectivos p ON p.Id = a.PeriodoId
                    WHERE e.Id = @EstudianteId 
                      AND p.Anio = @AnioLectivo
                      AND e.Estado = 1;

                    IF @AulaId IS NULL RETURN;

                    SELECT 
                        c.Id AS CuotaId,
                        d.Id AS CuotaDetalleId,
                        c.Concepto,
                        c.TipoCuota,
                        c.MontoIndividual AS MontoTotalCuota,
                        c.FechaVencimiento,
                        ISNULL(d.MontoPagado, 0) AS MontoPagado,
                        -- 🚀 Si está exonerado, el monto pendiente es 0.00
                        CASE 
                            WHEN UPPER(ISNULL(d.EstadoPago, 'PENDIENTE')) = 'EXONERADO' THEN 0.00
                            ELSE (c.MontoIndividual - ISNULL(d.MontoPagado, 0))
                        END AS MontoPendiente,
                        ISNULL(d.EstadoPago, 'PENDIENTE') AS EstadoPago,
                        -- 🚀 Estado Visual considerando EXONERADO
                        CASE 
                            WHEN UPPER(ISNULL(d.EstadoPago, 'PENDIENTE')) = 'EXONERADO' THEN 'EXONERADO'
                            WHEN UPPER(ISNULL(d.EstadoPago, 'PENDIENTE')) IN ('COMPLETO', 'PAGADO', 'VALIDADO') THEN 'PAGADO'
                            WHEN c.FechaVencimiento < CAST(GETDATE() AS DATE) THEN 'VENCIDO'
                            ELSE 'PENDIENTE'
                        END AS EstadoVisual,
                        d.MotivoExoneracion,
                        d.FechaUltimoPago
                    FROM Cuotas c
                    LEFT JOIN CuotaDetalleEstudiante d ON d.CuotaId = c.Id AND d.EstudianteId = @EstudianteId
                    WHERE c.AulaId = @AulaId
                      AND (c.Estado IS NULL OR CAST(c.Estado AS VARCHAR(50)) NOT IN ('INACTIVO', 'ELIMINADO', '0'))
                    ORDER BY c.FechaVencimiento ASC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Apoderado_ObtenerCuotasPendientes]
                    @EstudianteId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AulaId INT;

                    SELECT TOP 1 @AulaId = a.Id
                    FROM Estudiantes e
                    JOIN Aulas a ON a.Id = e.AulaId
                    JOIN PeriodosLectivos p ON p.Id = a.PeriodoId
                    WHERE e.Id = @EstudianteId 
                      AND p.Anio = @AnioLectivo
                      AND e.Estado = 1;

                    IF @AulaId IS NULL RETURN;

                    SELECT 
                        c.Id AS CuotaId,
                        c.Concepto,
                        c.TipoCuota,
                        c.MontoIndividual AS MontoTotalCuota,
                        c.FechaVencimiento,
                        ISNULL(d.MontoPagado, 0) AS MontoPagado,
                        (c.MontoIndividual - ISNULL(d.MontoPagado, 0)) AS MontoPendiente,
                        ISNULL(d.EstadoPago, 'PENDIENTE') AS EstadoPago,
                        CASE 
                            WHEN ISNULL(d.EstadoPago, 'PENDIENTE') IN ('COMPLETO', 'PAGADO') THEN 'PAGADO'
                            WHEN c.FechaVencimiento < GETDATE() THEN 'VENCIDO'
                            ELSE 'PENDIENTE'
                        END AS EstadoVisual,
                        d.FechaUltimoPago
                    FROM Cuotas c
                    LEFT JOIN CuotaDetalleEstudiante d ON d.CuotaId = c.Id AND d.EstudianteId = @EstudianteId
                    WHERE c.AulaId = @AulaId
                      AND (c.Estado IS NULL OR CAST(c.Estado AS VARCHAR(50)) NOT IN ('INACTIVO', 'ELIMINADO', '0'))
                    ORDER BY c.FechaVencimiento ASC;
                END
            ");
        }
    }
}
