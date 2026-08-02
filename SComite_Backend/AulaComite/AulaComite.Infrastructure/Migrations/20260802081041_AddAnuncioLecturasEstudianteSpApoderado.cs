using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnuncioLecturasEstudianteSpApoderado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Tabla de tracking individual para evitar lecturas duplicadas por apoderado/estudiante
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AnuncioLecturasEstudiante')
                BEGIN
                    CREATE TABLE AnuncioLecturasEstudiante (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        AnuncioId INT NOT NULL,
                        EstudianteId INT NOT NULL,
                        UsuarioApoderado VARCHAR(100) NOT NULL,
                        FechaLectura DATETIME NOT NULL DEFAULT (DATEADD(HOUR, -5, GETUTCDATE())),
                        CONSTRAINT FK_AnuncioLecturas_AnunciosComite FOREIGN KEY (AnuncioId) REFERENCES AnunciosComite(Id) ON DELETE CASCADE
                    );
                END
            ");

            // 2. SP para consultar los anuncios del aula desde la perspectiva del Apoderado
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Apoderado_ObtenerAnunciosMuro]
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
                        a.Id,
                        a.AulaId,
                        a.Titulo,
                        a.Contenido,
                        a.Categoria,
                        a.EsFijado,
                        a.UrlAdjunto,
                        a.UsuarioRegistro,
                        a.FechaPublicacion,
                        ISNULL(a.CantidadVistas, 0) AS CantidadVistas,
                        CASE 
                            WHEN al.Id IS NOT NULL THEN 1 
                            ELSE 0 
                        END AS Leido
                    FROM AnunciosComite a
                    LEFT JOIN AnuncioLecturasEstudiante al ON al.AnuncioId = a.Id AND al.EstudianteId = @EstudianteId
                    WHERE a.AulaId = @AulaId
                      AND (a.Estado IS NULL OR CAST(a.Estado AS VARCHAR(50)) NOT IN ('INACTIVO', 'ELIMINADO', '0'))
                    ORDER BY a.EsFijado DESC, a.FechaPublicacion DESC;
                END
            ");

            // 3. SP para registrar la lectura del Apoderado y sumar +1 a CantidadVistas en AnunciosComite
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Apoderado_RegistrarLecturaAnuncio]
                    @AnuncioId INT,
                    @EstudianteId INT,
                    @UsuarioApoderado VARCHAR(100)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Verificar si es la primera vez que este apoderado/estudiante lee este anuncio
                    IF NOT EXISTS (
                        SELECT 1 FROM AnuncioLecturasEstudiante 
                        WHERE AnuncioId = @AnuncioId AND EstudianteId = @EstudianteId
                    )
                    BEGIN
                        -- 1. Insertar el registro de lectura
                        INSERT INTO AnuncioLecturasEstudiante (AnuncioId, EstudianteId, UsuarioApoderado, FechaLectura)
                        VALUES (@AnuncioId, @EstudianteId, @UsuarioApoderado, DATEADD(HOUR, -5, GETUTCDATE()));

                        -- 2. Incrementar el contador global de vistas del anuncio
                        UPDATE AnunciosComite 
                        SET CantidadVistas = ISNULL(CantidadVistas, 0) + 1 
                        WHERE Id = @AnuncioId;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_Apoderado_ObtenerAnunciosMuro];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_Apoderado_RegistrarLecturaAnuncio];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS AnuncioLecturasEstudiante;");
        }
    }
}
