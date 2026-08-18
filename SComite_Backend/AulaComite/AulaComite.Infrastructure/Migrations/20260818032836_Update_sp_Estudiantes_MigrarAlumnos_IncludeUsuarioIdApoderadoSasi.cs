using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_sp_Estudiantes_MigrarAlumnos_IncludeUsuarioIdApoderadoSasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Estudiantes_MigrarAlumnos]
                    @AulaDestinoId INT,
                    @EstudianteIds NVARCHAR(MAX) -- Lista de IDs separados por coma: '1,2,3,4'
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;

                    BEGIN TRY
                        -- Tabla temporal con los IDs recibidos
                        DECLARE @IdsTable TABLE (Id INT);
                        
                        INSERT INTO @IdsTable (Id)
                        SELECT CAST(value AS INT) 
                        FROM STRING_SPLIT(@EstudianteIds, ',');

                        -- Insertar en Estudiantes para el nuevo AulaId conservando UsuarioIdApoderadoSasi
                        INSERT INTO Estudiantes (
                            AulaId,
                            TipoDocumento,
                            NumeroDocumento,
                            Nombres,
                            ApellidoPaterno,
                            ApellidoMaterno,
                            UsuarioIdApoderadoSasi, -- 🚀 Agregado para conservar el vínculo SASI
                            NombreApoderado,
                            TelefonoApoderado,
                            Estado,
                            FechaRegistro
                        )
                        SELECT 
                            @AulaDestinoId,
                            e.TipoDocumento,
                            e.NumeroDocumento,
                            e.Nombres,
                            e.ApellidoPaterno,
                            e.ApellidoMaterno,
                            e.UsuarioIdApoderadoSasi, -- 🚀 Se clona el ID del usuario SASI
                            e.NombreApoderado,
                            e.TelefonoApoderado,
                            1, -- Activo
                            GETDATE()
                        FROM Estudiantes e
                        INNER JOIN @IdsTable t ON e.Id = t.Id
                        WHERE NOT EXISTS (
                            SELECT 1 
                            FROM Estudiantes dest 
                            WHERE dest.AulaId = @AulaDestinoId 
                              AND dest.NumeroDocumento = e.NumeroDocumento
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
                        SELECT CAST(value AS INT) 
                        FROM STRING_SPLIT(@EstudianteIds, ',');

                        INSERT INTO Estudiantes (
                            AulaId,
                            TipoDocumento,
                            NumeroDocumento,
                            Nombres,
                            ApellidoPaterno,
                            ApellidoMaterno,
                            NombreApoderado,
                            TelefonoApoderado,
                            Estado,
                            FechaRegistro
                        )
                        SELECT 
                            @AulaDestinoId,
                            e.TipoDocumento,
                            e.NumeroDocumento,
                            e.Nombres,
                            e.ApellidoPaterno,
                            e.ApellidoMaterno,
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
                              AND dest.NumeroDocumento = e.NumeroDocumento
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
