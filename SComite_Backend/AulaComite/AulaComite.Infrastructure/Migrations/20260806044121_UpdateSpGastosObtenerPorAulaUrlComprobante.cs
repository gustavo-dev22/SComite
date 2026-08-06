using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpGastosObtenerPorAulaUrlComprobante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Gastos_ObtenerPorAula]
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        g.Id,
                        g.AulaId,
                        g.Concepto,
                        g.Categoria,
                        g.Monto,
                        g.FechaGasto,
                        g.TipoComprobante,
                        g.NumeroComprobante,
                        g.Proveedor,
                        g.Observacion,
                        g.UrlComprobante,
                        g.UsuarioRegistro,
                        g.FechaRegistro
                    FROM GastosComite g
                    WHERE g.AulaId = @AulaId
                    ORDER BY g.FechaGasto DESC, g.FechaRegistro DESC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Gastos_ObtenerPorAula]
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        g.Id,
                        g.AulaId,
                        g.Concepto,
                        g.Categoria,
                        g.Monto,
                        g.FechaGasto,
                        g.TipoComprobante,
                        g.NumeroComprobante,
                        g.Proveedor,
                        g.Observacion,
                        g.UsuarioRegistro,
                        g.FechaRegistro
                    FROM GastosComite g
                    WHERE g.AulaId = @AulaId
                    ORDER BY g.FechaGasto DESC, g.FechaRegistro DESC;
                END
            ");
        }
    }
}
