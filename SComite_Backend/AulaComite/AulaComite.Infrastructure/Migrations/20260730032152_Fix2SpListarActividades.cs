using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fix2SpListarActividades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Actividades_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        a.Id,
                        a.AulaId,
                        a.NombreActividad,
                        a.Descripcion,
                        a.FechaProgramada,
                        a.MontoPresupuestado,
                        a.CuotaSugeridaPorAlumno,
                        a.Estado,
                        a.FechaRegistro
                    FROM ActividadesComite a
                    INNER JOIN Aulas au ON a.AulaId = au.Id
                    LEFT JOIN PeriodosLectivos p ON au.PeriodoId = p.Id
                    WHERE a.AulaId = @AulaId
                      AND (@AnioLectivo IS NULL OR p.Anio = @AnioLectivo OR @AnioLectivo = 0)
                    ORDER BY a.FechaProgramada ASC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Actividades_ListarPorAula;");
        }
    }
}
