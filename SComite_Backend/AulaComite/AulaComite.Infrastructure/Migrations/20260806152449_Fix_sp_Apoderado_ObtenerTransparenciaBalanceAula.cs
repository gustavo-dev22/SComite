using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fix_sp_Apoderado_ObtenerTransparenciaBalanceAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Apoderado_ObtenerTransparenciaBalanceAula
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- 1. Resumen de Cajas (Ingresos Recaudados/Pagados y Egresos del Aula)
                    SELECT 
                        ISNULL((
                            SELECT SUM(d.MontoPagado) 
                            FROM CuotaDetalleEstudiante d
                            INNER JOIN Cuotas c ON d.CuotaId = c.Id
                            WHERE c.AulaId = @AulaId 
                              AND d.EstadoPago IN ('PAGADO', 'VALIDADO')
                        ), 0) AS TotalIngresos,
                        ISNULL((
                            SELECT SUM(g.Monto) 
                            FROM GastosComite g 
                            WHERE g.AulaId = @AulaId
                        ), 0) AS TotalEgresos;

                    -- 2. Listado de Egresos / Gastos
                    SELECT 
                        g.Id,
                        g.FechaGasto,
                        g.Concepto,
                        g.Categoria,
                        g.Monto,
                        g.Proveedor,
                        g.TipoComprobante,
                        g.NumeroComprobante,
                        g.UrlComprobante
                    FROM GastosComite g
                    WHERE g.AulaId = @AulaId
                    ORDER BY g.FechaGasto DESC;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_Apoderado_ObtenerTransparenciaBalanceAula')
                    DROP PROCEDURE sp_Apoderado_ObtenerTransparenciaBalanceAula;
            ");
        }
    }
}
