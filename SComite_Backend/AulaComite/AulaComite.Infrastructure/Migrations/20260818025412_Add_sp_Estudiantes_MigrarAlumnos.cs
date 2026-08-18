using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_sp_Estudiantes_MigrarAlumnos : Migration
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

                        -- Insertar en Estudiantes para el nuevo AulaId
                        -- Validando no duplicar si ya existe el mismo documento en el aula de destino
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
                IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_Estudiantes_MigrarAlumnos')
                    DROP PROCEDURE sp_Estudiantes_MigrarAlumnos;
            ");
        }
    }
}
