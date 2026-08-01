using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixModuloInstitucionEducativaGuardar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_InstitucionEducativa_Guardar
                    @NombreInstitucion NVARCHAR(200),
                    @Direccion NVARCHAR(250) = NULL,
                    @UrlLogo NVARCHAR(MAX) = NULL,
                    @UsuarioActualizacion VARCHAR(100)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @FechaActual DATETIME2 = DATEADD(HOUR, -5, GETUTCDATE());

                    IF EXISTS (SELECT 1 FROM InstitucionEducativa)
                    BEGIN
                        UPDATE TOP (1) InstitucionEducativa
                        SET NombreInstitucion = @NombreInstitucion,
                            Direccion = @Direccion,
                            UrlLogo = @UrlLogo,
                            FechaActualizacion = @FechaActual,
                            UsuarioActualizacion = @UsuarioActualizacion;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO InstitucionEducativa (
                            NombreInstitucion, Direccion, UrlLogo, FechaActualizacion, UsuarioActualizacion
                        ) VALUES (
                            @NombreInstitucion, @Direccion, @UrlLogo, @FechaActual, @UsuarioActualizacion
                        );
                    END

                    -- Devolver la fecha actualizada para refrescar la UI inmediatamente
                    SELECT TOP 1 FechaActualizacion FROM InstitucionEducativa;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
