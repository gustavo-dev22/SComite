using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_sp_Estudiantes_MigrarAlumnos_DetailedResult : Migration
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

                        DECLARE @TotalSolicitados INT = (SELECT COUNT(1) FROM @IdsTable);

                        -- 1. Identificar estudiantes que YA EXISTEN en el aula de destino (Omitidos)
                        DECLARE @Omitidos TABLE (
                            EstudianteId INT,
                            NombreCompleto NVARCHAR(250),
                            Motivo NVARCHAR(250)
                        );

                        INSERT INTO @Omitidos (EstudianteId, NombreCompleto, Motivo)
                        SELECT 
                            e.Id,
                            (e.ApellidoPaterno + ' ' + e.ApellidoMaterno + ', ' + e.Nombres),
                            'Ya se encuentra registrado en el aula de destino'
                        FROM Estudiantes e
                        INNER JOIN @IdsTable t ON e.Id = t.Id
                        WHERE EXISTS (
                            SELECT 1 
                            FROM Estudiantes dest 
                            WHERE dest.AulaId = @AulaDestinoId 
                              AND LTRIM(RTRIM(dest.NumeroDocumento)) = LTRIM(RTRIM(e.NumeroDocumento))
                              AND LTRIM(RTRIM(dest.Nombres)) = LTRIM(RTRIM(e.Nombres))
                              AND LTRIM(RTRIM(dest.ApellidoPaterno)) = LTRIM(RTRIM(e.ApellidoPaterno))
                              AND ISNULL(LTRIM(RTRIM(dest.ApellidoMaterno)), '') = ISNULL(LTRIM(RTRIM(e.ApellidoMaterno)), '')
                              AND dest.Estado = 1
                        );

                        -- 2. Reactivar los inactivos
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

                        -- 3. Insertar nuevos
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
                        DECLARE @TotalMigrados INT = @FilasInsertadas + @FilasReactivadas;
                        DECLARE @TotalOmitidos INT = (SELECT COUNT(1) FROM @Omitidos);

                        COMMIT TRANSACTION;

                        -- Resultado 1: Totales
                        SELECT 
                            @TotalSolicitados AS Solicitados,
                            @TotalMigrados AS Migrados,
                            @TotalOmitidos AS Omitidos;

                        -- Resultado 2: Detalles de los omitidos
                        SELECT 
                            NombreCompleto,
                            Motivo
                        FROM @Omitidos;

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

        }
    }
}
