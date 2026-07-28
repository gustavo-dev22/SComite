using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Actualizacion_SPs_Aulas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. SP Actualizar (SET NOCOUNT OFF)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Aulas_Actualizar
                    @Id INT,
                    @PeriodoId INT,
                    @Nivel VARCHAR(30),
                    @Grado VARCHAR(50),
                    @Seccion VARCHAR(10)
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    UPDATE Aulas
                    SET PeriodoId = @PeriodoId,
                        Nivel = @Nivel,
                        Grado = @Grado,
                        Seccion = @Seccion
                    WHERE Id = @Id;
                END
            ");

            // 2. SP Eliminar Lógico (SET NOCOUNT OFF)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Aulas_EliminarLogico
                    @Id INT
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    UPDATE Aulas
                    SET Estado = 0
                    WHERE Id = @Id;
                END
            ");

            // 3. SP Obtener Todas (Trae activas e inactivas)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Aulas_ObtenerTodas
                    @PeriodoId INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT a.Id, a.PeriodoId, a.Nivel, a.Grado, a.Seccion, 
                           (a.Nivel + ' - ' + a.Grado + ' ' + a.Seccion) AS NombreDisplay,
                           a.Estado, p.Anio AS AnioPeriodo
                    FROM Aulas a
                    INNER JOIN PeriodosLectivos p ON a.PeriodoId = p.Id
                    WHERE (@PeriodoId IS NULL OR a.PeriodoId = @PeriodoId)
                    ORDER BY p.Anio DESC, a.Nivel, a.Grado, a.Seccion;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
