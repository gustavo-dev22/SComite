using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpEstudiantesCargaMasiva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Tipo de Tabla para recibir la lista masiva desde .NET / Dapper
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'EstudianteCargaMasivaType')
                BEGIN
                    CREATE TYPE EstudianteCargaMasivaType AS TABLE
                    (
                        TipoDocumento VARCHAR(10),
                        NumeroDocumento VARCHAR(20),
                        Nombres VARCHAR(100),
                        ApellidoPaterno VARCHAR(100),
                        ApellidoMaterno VARCHAR(100),
                        UsuarioIdApoderadoSasi VARCHAR(100),
                        NombreApoderado VARCHAR(200),
                        TelefonoApoderado VARCHAR(20)
                    );
                END
            ");

            // 2. Stored Procedure para procesar la carga masiva
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Estudiantes_CargaMasiva]
                    @AulaId INT,
                    @Estudiantes EstudianteCargaMasivaType READONLY
                AS
                BEGIN
                    SET NOCOUNT ON;

                    BEGIN TRANSACTION;

                    BEGIN TRY
                        -- Insertar estudiantes evitando duplicar Número de Documento en el mismo Aula
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
                            @AulaId,
                            UPPER(LTRIM(RTRIM(e.TipoDocumento))),
                            LTRIM(RTRIM(e.NumeroDocumento)),
                            UPPER(LTRIM(RTRIM(e.Nombres))),
                            UPPER(LTRIM(RTRIM(e.ApellidoPaterno))),
                            UPPER(LTRIM(RTRIM(e.ApellidoMaterno))),
                            NULLIF(LTRIM(RTRIM(e.UsuarioIdApoderadoSasi)), ''),
                            NULLIF(LTRIM(RTRIM(e.NombreApoderado)), ''),
                            NULLIF(LTRIM(RTRIM(e.TelefonoApoderado)), ''),
                            1,
                            DATEADD(HOUR, -5, GETUTCDATE())
                        FROM @Estudiantes e
                        WHERE NOT EXISTS (
                            SELECT 1 FROM Estudiantes est 
                            WHERE est.AulaId = @AulaId 
                              AND est.NumeroDocumento = LTRIM(RTRIM(e.NumeroDocumento))
                        );

                        COMMIT TRANSACTION;

                        -- Retorna el total de registros procesados e insertados
                        SELECT @@ROWCOUNT AS RegistrosInsertados;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_Estudiantes_CargaMasiva];");
            migrationBuilder.Sql("DROP TYPE IF EXISTS EstudianteCargaMasivaType;");
        }
    }
}
