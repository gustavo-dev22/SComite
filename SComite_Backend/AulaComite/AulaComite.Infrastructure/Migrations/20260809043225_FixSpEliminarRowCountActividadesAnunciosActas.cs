using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSpEliminarRowCountActividadesAnunciosActas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Actividades_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DELETE FROM ActividadesComite WHERE Id = @Id AND AulaId = @AulaId;
                    SELECT @@ROWCOUNT;
                END
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Anuncios_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE AnunciosComite SET Estado = 0 WHERE Id = @Id AND AulaId = @AulaId;
                    SELECT @@ROWCOUNT;
                END
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_ActasAsamblea_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE ActasAsambleaComite SET Estado = 0 WHERE Id = @Id AND AulaId = @AulaId;
                    SELECT @@ROWCOUNT;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Actividades_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DELETE FROM ActividadesComite WHERE Id = @Id AND AulaId = @AulaId;
                END
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Anuncios_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE AnunciosComite SET Estado = 0 WHERE Id = @Id AND AulaId = @AulaId;
                END
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_ActasAsamblea_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE ActasAsambleaComite SET Estado = 0 WHERE Id = @Id AND AulaId = @AulaId;
                END
            ");
        }
    }
}
