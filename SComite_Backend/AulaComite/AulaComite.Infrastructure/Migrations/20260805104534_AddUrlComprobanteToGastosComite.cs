using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUrlComprobanteToGastosComite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Agregar la nueva columna UrlComprobante a la tabla GastosComite
            migrationBuilder.AddColumn<string>(
                name: "UrlComprobante",
                table: "GastosComite",
                type: "VARCHAR(500)",
                nullable: true);

            // 2. Actualizar el Stored Procedure sp_Gastos_Registrar para incluir @UrlComprobante
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Gastos_Registrar]
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

                    INSERT INTO GastosComite (
                        AulaId, Concepto, Categoria, Monto, FechaGasto, 
                        TipoComprobante, NumeroComprobante, Proveedor, Observacion, 
                        UrlComprobante, UsuarioRegistro
                    )
                    VALUES (
                        @AulaId, @Concepto, @Categoria, @Monto, @FechaGasto, 
                        @TipoComprobante, @NumeroComprobante, @Proveedor, @Observacion, 
                        @UrlComprobante, @UsuarioRegistro
                    );

                    SELECT SCOPE_IDENTITY();
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UrlComprobante",
                table: "GastosComite");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Gastos_Registrar]
                    @AulaId INT,
                    @Concepto VARCHAR(150),
                    @Categoria VARCHAR(50),
                    @Monto DECIMAL(10,2),
                    @FechaGasto DATE,
                    @TipoComprobante VARCHAR(30),
                    @NumeroComprobante VARCHAR(50) = NULL,
                    @Proveedor NVARCHAR(150) = NULL,
                    @Observacion NVARCHAR(300) = NULL,
                    @UsuarioRegistro NVARCHAR(150)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    INSERT INTO GastosComite (AulaId, Concepto, Categoria, Monto, FechaGasto, TipoComprobante, NumeroDocumento, Proveedor, Observacion, UsuarioRegistro)
                    VALUES (@AulaId, @Concepto, @Categoria, @Monto, @FechaGasto, @TipoComprobante, @NumeroComprobante, @Proveedor, @Observacion, @UsuarioRegistro);

                    SELECT SCOPE_IDENTITY();
                END
            ");
        }
    }
}
