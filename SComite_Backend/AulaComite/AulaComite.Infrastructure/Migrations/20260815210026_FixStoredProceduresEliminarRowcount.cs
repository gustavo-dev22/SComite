using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixStoredProceduresEliminarRowcount : Migration
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
                    SET NOCOUNT OFF;
                    DELETE FROM ActividadesComite WHERE Id = @Id AND AulaId = @AulaId;
                END
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Anuncios_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT OFF;
                    UPDATE AnunciosComite SET Estado = 0 WHERE Id = @Id AND AulaId = @AulaId;
                END
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_ActasAsamblea_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT OFF;
                    UPDATE ActasAsambleaComite SET Estado = 0 WHERE Id = @Id AND AulaId = @AulaId;
                END
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Donaciones_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT OFF;
                    DELETE FROM DonacionesComite WHERE Id = @Id AND AulaId = @AulaId;
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

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Donaciones_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DELETE FROM DonacionesComite WHERE Id = @Id AND AulaId = @AulaId;
                    SELECT @@ROWCOUNT;
                END
            ");
        }
    }
}