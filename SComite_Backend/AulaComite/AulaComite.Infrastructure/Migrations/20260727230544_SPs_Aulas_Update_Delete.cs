using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SPs_Aulas_Update_Delete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SP: Actualizar Aula completa
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_Aulas_Actualizar
                    @Id INT,
                    @PeriodoId INT,
                    @Nivel VARCHAR(30),
                    @Grado VARCHAR(50),
                    @Seccion VARCHAR(10)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE Aulas
                    SET PeriodoId = @PeriodoId,
                        Nivel = @Nivel,
                        Grado = @Grado,
                        Seccion = @Seccion
                    WHERE Id = @Id;
                END
            ");

            // SP: Eliminación Lógica de Aula (Cambiar Estado = 0)
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_Aulas_EliminarLogico
                    @Id INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE Aulas
                    SET Estado = 0
                    WHERE Id = @Id;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Aulas_Actualizar");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Aulas_EliminarLogico");
        }
    }
}
