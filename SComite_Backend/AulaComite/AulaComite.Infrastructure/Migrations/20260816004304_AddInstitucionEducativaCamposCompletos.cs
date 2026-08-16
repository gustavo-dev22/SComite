using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitucionEducativaCamposCompletos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🛡️ T4.5: Se agregan los campos que el comando ya aceptaba pero que se
            // descartaban al persistir (CodigoModular, LemaInstitucional, NombreDirector,
            // Telefono, CorreoContacto). Se crean de forma IDEMPOTENTE (IF COL_LENGTH).
            migrationBuilder.Sql(@"
                IF COL_LENGTH('InstitucionEducativa', 'CodigoModular') IS NULL
                    ALTER TABLE InstitucionEducativa ADD CodigoModular NVARCHAR(20) NULL;

                IF COL_LENGTH('InstitucionEducativa', 'LemaInstitucional') IS NULL
                    ALTER TABLE InstitucionEducativa ADD LemaInstitucional NVARCHAR(300) NULL;

                IF COL_LENGTH('InstitucionEducativa', 'NombreDirector') IS NULL
                    ALTER TABLE InstitucionEducativa ADD NombreDirector NVARCHAR(150) NULL;

                IF COL_LENGTH('InstitucionEducativa', 'Telefono') IS NULL
                    ALTER TABLE InstitucionEducativa ADD Telefono NVARCHAR(20) NULL;

                IF COL_LENGTH('InstitucionEducativa', 'CorreoContacto') IS NULL
                    ALTER TABLE InstitucionEducativa ADD CorreoContacto NVARCHAR(150) NULL;
            ");

            // 🛡️ T4.5: sp_InstitucionEducativa_Guardar persistiendo TODOS los campos del comando.
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_InstitucionEducativa_Guardar
                    @NombreInstitucion NVARCHAR(200),
                    @CodigoModular NVARCHAR(20) = NULL,
                    @LemaInstitucional NVARCHAR(300) = NULL,
                    @NombreDirector NVARCHAR(150) = NULL,
                    @Direccion NVARCHAR(250) = NULL,
                    @Telefono NVARCHAR(20) = NULL,
                    @CorreoContacto NVARCHAR(150) = NULL,
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
                            CodigoModular = @CodigoModular,
                            LemaInstitucional = @LemaInstitucional,
                            NombreDirector = @NombreDirector,
                            Direccion = @Direccion,
                            Telefono = @Telefono,
                            CorreoContacto = @CorreoContacto,
                            UrlLogo = @UrlLogo,
                            FechaActualizacion = @FechaActual,
                            UsuarioActualizacion = @UsuarioActualizacion;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO InstitucionEducativa (
                            NombreInstitucion, CodigoModular, LemaInstitucional, NombreDirector,
                            Direccion, Telefono, CorreoContacto, UrlLogo, FechaActualizacion, UsuarioActualizacion
                        ) VALUES (
                            @NombreInstitucion, @CodigoModular, @LemaInstitucional, @NombreDirector,
                            @Direccion, @Telefono, @CorreoContacto, @UrlLogo, @FechaActual, @UsuarioActualizacion
                        );
                    END

                    -- Devolver la fecha actualizada para refrescar la UI inmediatamente
                    SELECT TOP 1 FechaActualizacion FROM InstitucionEducativa;
                END
            ");

            // 🛡️ T4.5: sp_InstitucionEducativa_Obtener incluyendo los nuevos campos.
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_InstitucionEducativa_Obtener
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP 1
                        Id,
                        NombreInstitucion,
                        CodigoModular,
                        LemaInstitucional,
                        NombreDirector,
                        Direccion,
                        Telefono,
                        CorreoContacto,
                        UrlLogo,
                        FechaActualizacion,
                        UsuarioActualizacion
                    FROM InstitucionEducativa;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La reversión no elimina columnas para preservar los datos existentes; los SPs
            // son equivalentes en comportamiento salvo por los nuevos campos opcionales.
        }
    }
}
