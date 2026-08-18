using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_sp_Estudiantes_MigrarAlumnos_ValidacionNombreCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Estudiantes_MigrarAlumnos]
                    @AulaDestinoId INT,
                    @EstudianteIds NVARCHAR(MAX)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;

                    BEGIN TRY
                        DECLARE @IdsTable TABLE (Id INT);
                        
                        INSERT INTO @IdsTable (Id)
                        SELECT CAST(LTRIM(RTRIM(value)) AS INT) 
                        FROM STRING_SPLIT(@EstudianteIds, ',')
                        WHERE LTRIM(RTRIM(value)) <> '';

                        -- 1. Si el alumno exacto ya existía en el aula destino pero estaba inactivo (Estado = 0), reactivarlo
                        UPDATE dest
                        SET dest.Estado = 1,
                            dest.UsuarioIdApoderadoSasi = ISNULL(orig.UsuarioIdApoderadoSasi, dest.UsuarioIdApoderadoSasi),
                            dest.NombreApoderado = ISNULL(orig.NombreApoderado, dest.NombreApoderado),
                            dest.TelefonoApoderado = ISNULL(orig.TelefonoApoderado, dest.TelefonoApoderado)
                        FROM Estudiantes dest
                        INNER JOIN Estudiantes orig 
                            ON LTRIM(RTRIM(dest.NumeroDocumento)) = LTRIM(RTRIM(orig.NumeroDocumento))
                           AND LTRIM(RTRIM(dest.Nombres)) = LTRIM(RTRIM(orig.Nombres))
                           AND LTRIM(RTRIM(dest.ApellidoPaterno)) = LTRIM(RTRIM(orig.ApellidoPaterno))
                           AND ISNULL(LTRIM(RTRIM(dest.ApellidoMaterno)), '') = ISNULL(LTRIM(RTRIM(orig.ApellidoMaterno)), '')
                           AND dest.AulaId = @AulaDestinoId
                        INNER JOIN @IdsTable t 
                            ON orig.Id = t.Id
                        WHERE dest.Estado = 0;

                        DECLARE @FilasReactivadas INT = @@ROWCOUNT;

                        -- 2. Insertar estudiantes que NO existan en el aula destino con el mismo Documento + Nombres + Apellidos
                        INSERT INTO Estudiantes (
                            AulaId,
                            TipoDocumento,
                            NumeroDocumento,
                            Nombres,
                            ApellidoPaterno,
                            ApellidoMaterno,
                            UsuarioIdApoderadoSasi,
                            NombreApoderado,
                            TelefonoApoderado,
                            Estado,
                            FechaRegistro
                        )
                        SELECT 
                            @AulaDestinoId,
                            e.TipoDocumento,
                            LTRIM(RTRIM(e.NumeroDocumento)),
                            LTRIM(RTRIM(e.Nombres)),
                            LTRIM(RTRIM(e.ApellidoPaterno)),
                            LTRIM(RTRIM(e.ApellidoMaterno)),
                            e.UsuarioIdApoderadoSasi,
                            e.NombreApoderado,
                            e.TelefonoApoderado,
                            1,
                            GETDATE()
                        FROM Estudiantes e
                        INNER JOIN @IdsTable t ON e.Id = t.Id
                        WHERE NOT EXISTS (
                            SELECT 1 
                            FROM Estudiantes dest 
                            WHERE dest.AulaId = @AulaDestinoId 
                              AND LTRIM(RTRIM(dest.NumeroDocumento)) = LTRIM(RTRIM(e.NumeroDocumento))
                              AND LTRIM(RTRIM(dest.Nombres)) = LTRIM(RTRIM(e.Nombres))
                              AND LTRIM(RTRIM(dest.ApellidoPaterno)) = LTRIM(RTRIM(e.ApellidoPaterno))
                              AND ISNULL(LTRIM(RTRIM(dest.ApellidoMaterno)), '') = ISNULL(LTRIM(RTRIM(e.ApellidoMaterno)), '')
                        );

                        DECLARE @FilasInsertadas INT = @@ROWCOUNT;

                        COMMIT TRANSACTION;

                        SELECT (@FilasInsertadas + @FilasReactivadas) AS TotalMigrados;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Estudiantes_MigrarAlumnos]
                    @AulaDestinoId INT,
                    @EstudianteIds NVARCHAR(MAX)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;

                    BEGIN TRY
                        DECLARE @IdsTable TABLE (Id INT);
                        
                        INSERT INTO @IdsTable (Id)
                        SELECT CAST(LTRIM(RTRIM(value)) AS INT) 
                        FROM STRING_SPLIT(@EstudianteIds, ',')
                        WHERE LTRIM(RTRIM(value)) <> '';

                        INSERT INTO Estudiantes (
                            AulaId,
                            TipoDocumento,
                            NumeroDocumento,
                            Nombres,
                            ApellidoPaterno,
                            ApellidoMaterno,
                            UsuarioIdApoderadoSasi,
                            NombreApoderado,
                            TelefonoApoderado,
                            Estado,
                            FechaRegistro
                        )
                        SELECT 
                            @AulaDestinoId,
                            e.TipoDocumento,
                            LTRIM(RTRIM(e.NumeroDocumento)),
                            e.Nombres,
                            e.ApellidoPaterno,
                            e.ApellidoMaterno,
                            e.UsuarioIdApoderadoSasi,
                            e.NombreApoderado,
                            e.TelefonoApoderado,
                            1,
                            GETDATE()
                        FROM Estudiantes e
                        INNER JOIN @IdsTable t ON e.Id = t.Id
                        WHERE NOT EXISTS (
                            SELECT 1 
                            FROM Estudiantes dest 
                            WHERE dest.AulaId = @AulaDestinoId 
                              AND LTRIM(RTRIM(dest.NumeroDocumento)) = LTRIM(RTRIM(e.NumeroDocumento))
                        );

                        DECLARE @FilasInsertadas INT = @@ROWCOUNT;

                        COMMIT TRANSACTION;

                        SELECT @FilasInsertadas AS TotalMigrados;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END;
            ");
        }
    }
}
