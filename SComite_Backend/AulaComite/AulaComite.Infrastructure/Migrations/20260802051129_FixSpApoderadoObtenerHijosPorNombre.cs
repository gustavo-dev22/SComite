using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSpApoderadoObtenerHijosPorNombre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Apoderado_ObtenerHijos]
                    @UsuarioApoderado VARCHAR(100),
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SET @UsuarioApoderado = LTRIM(RTRIM(@UsuarioApoderado));

                    SELECT 
                        e.Id AS EstudianteId,
                        (e.Nombres + ' ' + e.ApellidoPaterno + ' ' + ISNULL(e.ApellidoMaterno, '')) AS NombreEstudiante,
                        a.Id AS AulaId,
                        (a.Nivel + ' - ' + CAST(a.Grado AS VARCHAR(50)) + ' ""' + a.Seccion + '""') AS NombreAula,
                        a.Nivel,
                        CAST(a.Grado AS VARCHAR(50)) AS Grado,
                        a.Seccion,
                        -- Datos del Tesorero del Aula
                        ISNULL(ci.NombreCompleto, 'Tesorero de Aula') AS TesoreroNombre
                    FROM Estudiantes e
                    JOIN Aulas a ON a.Id = e.AulaId
                    JOIN PeriodosLectivos p ON p.Id = a.PeriodoId
                    LEFT JOIN ComiteIntegrantes ci ON ci.AulaId = a.Id 
                        AND UPPER(ci.Cargo) LIKE '%TESORERO%' 
                        AND (ci.Estado = 1 OR ci.Estado IS NULL)
                    WHERE (
                            LTRIM(RTRIM(ISNULL(e.NombreApoderado, ''))) = @UsuarioApoderado
                         OR LTRIM(RTRIM(ISNULL(e.NombreApoderado, ''))) LIKE '%' + @UsuarioApoderado + '%'
                    )
                      AND p.Anio = @AnioLectivo
                      AND e.Estado = 1
                    ORDER BY a.Nivel ASC, a.Grado ASC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
