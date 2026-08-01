using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpAnunciosGuardarUsuarioRegistro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Anuncios_Guardar
                    @Id INT = 0,
                    @AulaId INT,
                    @Titulo NVARCHAR(150),
                    @Contenido NVARCHAR(MAX),
                    @Categoria VARCHAR(30),
                    @EsFijado BIT = 0,
                    @UrlAdjunto NVARCHAR(500) = NULL,
                    @UsuarioRegistro VARCHAR(100)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Id = 0
                    BEGIN
                        INSERT INTO AnunciosComite (AulaId, Titulo, Contenido, Categoria, EsFijado, UrlAdjunto, UsuarioRegistro)
                        VALUES (@AulaId, @Titulo, @Contenido, UPPER(@Categoria), @EsFijado, @UrlAdjunto, @UsuarioRegistro);

                        SELECT CAST(SCOPE_IDENTITY() AS INT);
                    END
                    ELSE
                    BEGIN
                        -- 🚀 Se agrega UsuarioRegistro en el UPDATE
                        UPDATE AnunciosComite
                        SET Titulo = @Titulo,
                            Contenido = @Contenido,
                            Categoria = UPPER(@Categoria),
                            EsFijado = @EsFijado,
                            UrlAdjunto = @UrlAdjunto,
                            UsuarioRegistro = @UsuarioRegistro
                        WHERE Id = @Id AND AulaId = @AulaId;

                        SELECT @Id;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
