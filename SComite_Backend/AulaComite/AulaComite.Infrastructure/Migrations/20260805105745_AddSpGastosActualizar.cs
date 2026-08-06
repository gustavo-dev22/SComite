using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpGastosActualizar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Gastos_Actualizar]
                    @Id INT,
                    @AulaId INT,
                    @Concepto VARCHAR(150),
                    @Categoria VARCHAR(50),
                    @Monto DECIMAL(10,2),
                    @FechaGasto DATE,
                    @TipoComprobante VARCHAR(30),
                    @NumeroComprobante VARCHAR(50) = NULL,
                    @Proveedor NVARCHAR(150) = NULL,
                    @Observacion NVARCHAR(300) = NULL,
                    @UrlComprobante VARCHAR(500) = NULL,
                    @UsuarioRegistro NVARCHAR(150)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    UPDATE GastosComite
                    SET Concepto = @Concepto,
                        Categoria = @Categoria,
                        Monto = @Monto,
                        FechaGasto = @FechaGasto,
                        TipoComprobante = @TipoComprobante,
                        NumeroComprobante = @NumeroComprobante,
                        Proveedor = @Proveedor,
                        Observacion = @Observacion,
                        UrlComprobante = ISNULL(@UrlComprobante, UrlComprobante)
                    WHERE Id = @Id AND AulaId = @AulaId;

                    SELECT @@ROWCOUNT;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_Gastos_Actualizar];");
        }
    }
}
