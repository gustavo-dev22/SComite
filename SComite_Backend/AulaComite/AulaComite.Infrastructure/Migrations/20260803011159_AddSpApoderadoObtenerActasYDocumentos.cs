using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpApoderadoObtenerActasYDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Apoderado_ObtenerActasYDocumentos]
                    @EstudianteId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AulaId INT;

                    SELECT TOP 1 @AulaId = a.Id
                    FROM Estudiantes e
                    JOIN Aulas a ON a.Id = e.AulaId
                    JOIN PeriodosLectivos p ON p.Id = a.PeriodoId
                    WHERE e.Id = @EstudianteId 
                      AND p.Anio = @AnioLectivo
                      AND e.Estado = 1;

                    IF @AulaId IS NULL RETURN;

                    -- 1. Obtener Actas de Asamblea solo en estado APROBADA
                    SELECT 
                        a.Id,
                        a.AulaId,
                        a.NumeroActa,
                        a.Titulo,
                        a.FechaReunion,
                        a.AgendaAcuerdos,
                        a.EstadoActa,
                        a.UrlDocumentoPdf,
                        a.UsuarioRegistro,
                        a.FechaRegistro,
                        a.UsuarioActualizacion,
                        a.FechaActualizacion
                    FROM ActasAsambleaComite a
                    WHERE a.AulaId = @AulaId
                      AND UPPER(a.EstadoActa) = 'APROBADA'
                      AND (a.Estado IS NULL OR CAST(a.Estado AS VARCHAR(50)) NOT IN ('INACTIVO', 'ELIMINADO', '0'))
                    ORDER BY a.FechaReunion DESC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_Apoderado_ObtenerActasYDocumentos];");
        }
    }
}
