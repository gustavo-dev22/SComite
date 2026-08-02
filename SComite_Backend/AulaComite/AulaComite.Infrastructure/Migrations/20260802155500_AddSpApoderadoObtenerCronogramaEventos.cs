using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpApoderadoObtenerCronogramaEventos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Apoderado_ObtenerCronogramaEventos]
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

                    SELECT 
                        ca.Id,
                        ca.AulaId,
                        ca.NombreActividad,
                        ca.Descripcion,
                        ca.FechaProgramada,
                        ISNULL(ca.MontoPresupuestado, 0) AS MontoPresupuestado,
                        ISNULL(ca.CuotaSugeridaPorAlumno, 0) AS CuotaSugeridaPorAlumno,
                        ISNULL(ca.Estado, 'PLANIFICADA') AS Estado
                    FROM ActividadesComite ca
                    WHERE ca.AulaId = @AulaId
                      AND (ca.Estado IS NULL OR CAST(ca.Estado AS VARCHAR(50)) NOT IN ('INACTIVO', 'ELIMINADO', '0'))
                    ORDER BY ca.FechaProgramada ASC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_Apoderado_ObtenerCronogramaEventos];");
        }
    }
}
