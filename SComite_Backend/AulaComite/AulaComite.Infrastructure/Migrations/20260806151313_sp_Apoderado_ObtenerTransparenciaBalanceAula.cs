using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class sp_Apoderado_ObtenerTransparenciaBalanceAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_Apoderado_ObtenerTransparenciaBalanceAula')
                    DROP PROCEDURE sp_Apoderado_ObtenerTransparenciaBalanceAula;
                GO

                CREATE PROCEDURE sp_Apoderado_ObtenerTransparenciaBalanceAula
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- 1. Resumen de Cajas (Ingresos Validados y Egresos del Aula)
                    SELECT 
                        ISNULL((
                            SELECT SUM(p.MontoPagado) 
                            FROM PagosCuota p 
                            INNER JOIN Cuotas c ON p.CuotaId = c.Id 
                            WHERE c.AulaId = @AulaId AND p.Estado = 'VALIDADO'
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
                GO
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
