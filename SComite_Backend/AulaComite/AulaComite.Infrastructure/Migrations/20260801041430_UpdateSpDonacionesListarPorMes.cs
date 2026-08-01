using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpDonacionesListarPorMes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Donaciones_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL -- NULL o 0 = Todo el año
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        d.Id,
                        d.AulaId,
                        d.Donante,
                        d.Monto,
                        d.FechaDonacion,
                        d.Concepto,
                        d.Observacion,
                        d.FechaRegistro
                    FROM DonacionesComite d
                    WHERE d.AulaId = @AulaId
                      AND YEAR(d.FechaDonacion) = @AnioLectivo
                      AND (@Mes IS NULL OR @Mes = 0 OR MONTH(d.FechaDonacion) = @Mes)
                    ORDER BY d.FechaDonacion DESC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Donaciones_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        d.Id,
                        d.AulaId,
                        d.Donante,
                        d.Monto,
                        d.FechaDonacion,
                        d.Concepto,
                        d.Observacion,
                        d.FechaRegistro
                    FROM DonacionesComite d
                    WHERE d.AulaId = @AulaId
                      AND YEAR(d.FechaDonacion) = @AnioLectivo
                    ORDER BY d.FechaDonacion DESC;
                END
            ");
        }
    }
}
