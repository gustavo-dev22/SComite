using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_sp_Cuotas_ObtenerPorAula_AddEstudiantesExonerados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Cuotas_ObtenerPorAula]
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
                        -- 🚀 El monto esperado solo suma a los que NO están exonerados
                        ISNULL(SUM(CASE WHEN ISNULL(cd.EstadoPago, 'PENDIENTE') <> 'EXONERADO' THEN cd.MontoAsignado ELSE 0.00 END), 0.00) AS TotalMontoEsperado,
                        ISNULL(SUM(cd.MontoPagado), 0.00) AS TotalMontoRecaudado,
                        ISNULL(SUM(CASE WHEN cd.EstadoPago IN ('COMPLETO', 'PAGADO', 'VALIDADO') THEN 1 ELSE 0 END), 0) AS EstudiantesAlDia,
                        ISNULL(SUM(CASE WHEN cd.EstadoPago IN ('PENDIENTE', 'PARCIAL') THEN 1 ELSE 0 END), 0) AS EstudiantesPendientes,
                        -- 🚀 Conteo de estudiantes exonerados
                        ISNULL(SUM(CASE WHEN cd.EstadoPago = 'EXONERADO' THEN 1 ELSE 0 END), 0) AS EstudiantesExonerados
                    FROM Cuotas c
                    LEFT JOIN CuotaDetalleEstudiante cd ON c.Id = cd.CuotaId
                    WHERE c.AulaId = @AulaId
                    GROUP BY c.Id, c.AulaId, c.Concepto, c.MontoIndividual, c.FechaVencimiento, c.Estado, c.Observacion, c.FechaCreacion, c.TipoCuota, c.MesCorrespondiente
                    ORDER BY c.FechaVencimiento ASC;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Cuotas_ObtenerPorAula]
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
                    ORDER BY c.FechaVencimiento ASC;
                END;
            ");
        }
    }
}
