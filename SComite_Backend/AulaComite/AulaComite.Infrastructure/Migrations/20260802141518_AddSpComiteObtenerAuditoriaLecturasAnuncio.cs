using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpComiteObtenerAuditoriaLecturasAnuncio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Comite_ObtenerAuditoriaLecturasAnuncio]
                    @AnuncioId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AulaId INT;
                    SELECT TOP 1 @AulaId = AulaId FROM AnunciosComite WHERE Id = @AnuncioId;

                    IF @AulaId IS NULL RETURN;

                    SELECT 
                        e.Id AS EstudianteId,
                        (e.Nombres + ' ' + e.ApellidoPaterno + ' ' + ISNULL(e.ApellidoMaterno, '')) AS NombreEstudiante,
                        ISNULL(e.NombreApoderado, 'Sin Apoderado') AS NombreApoderado,
                        ISNULL(e.TelefonoApoderado, '') AS TelefonoApoderado,
                        CASE 
                            WHEN al.Id IS NOT NULL THEN CAST(1 AS BIT)
                            ELSE CAST(0 AS BIT)
                        END AS Leido,
                        al.FechaLectura
                    FROM Estudiantes e
                    LEFT JOIN AnuncioLecturasEstudiante al ON al.EstudianteId = e.Id AND al.AnuncioId = @AnuncioId
                    WHERE e.AulaId = @AulaId
                      AND e.Estado = 1
                    ORDER BY Leido DESC, e.ApellidoPaterno ASC, e.Nombres ASC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_Comite_ObtenerAuditoriaLecturasAnuncio];");
        }
    }
}
