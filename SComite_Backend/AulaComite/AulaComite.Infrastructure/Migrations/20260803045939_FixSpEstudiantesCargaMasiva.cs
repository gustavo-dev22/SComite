using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSpEstudiantesCargaMasiva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Estudiantes_CargaMasiva]
                    @AulaId INT,
                    @Estudiantes EstudianteCargaMasivaType READONLY
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @Insertados INT = 0;

                    BEGIN TRANSACTION;

                    BEGIN TRY
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
                            UPPER(LTRIM(RTRIM(ISNULL(e.TipoDocumento, 'DNI')))),
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
                              AND est.Estado = 1
                        );

                        -- Guardar recuento exacto de inserciones realizadas
                        SET @Insertados = @@ROWCOUNT;

                        COMMIT TRANSACTION;

                        SELECT @Insertados AS RegistrosInsertados;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
