using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpApoderadoObtenerHijosTelefonoEstudiantes : Migration
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
                        -- Datos del Tesorero obtenidos cruzando ComiteIntegrantes con el Apoderado de Estudiantes
                        ISNULL(ci.NombreCompleto, 'Tesorero de Aula') AS TesoreroNombre,
                        ISNULL(estTesorero.TelefonoApoderado, '') AS TesoreroTelefono,
                        ISNULL(estTesorero.TelefonoApoderado, '') AS NumeroYapePlin
                    FROM Estudiantes e
                    JOIN Aulas a ON a.Id = e.AulaId
                    JOIN PeriodosLectivos p ON p.Id = a.PeriodoId
                    LEFT JOIN ComiteIntegrantes ci ON ci.AulaId = a.Id 
                        AND UPPER(ci.Cargo) LIKE '%TESORERO%' 
                        AND ci.Estado = 1
                    -- 🚀 JOIN para obtener el número telefónico directamente desde la tabla Estudiantes
                    LEFT JOIN Estudiantes estTesorero ON estTesorero.AulaId = a.Id 
                        AND (estTesorero.UsuarioIdApoderadoSasi = ci.UsuarioIdSasi OR estTesorero.NombreApoderado = ci.NombreCompleto)
                        AND estTesorero.Estado = 1
                    WHERE (
                            LTRIM(RTRIM(ISNULL(e.NombreApoderado, ''))) = @UsuarioApoderado
                         OR LTRIM(RTRIM(ISNULL(e.NombreApoderado, ''))) LIKE '%' + @UsuarioApoderado + '%'
                         OR LTRIM(RTRIM(ISNULL(e.UsuarioIdApoderadoSasi, ''))) = @UsuarioApoderado
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
