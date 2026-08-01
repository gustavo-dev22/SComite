using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloInstitucionEducativa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear Tabla InstitucionEducativa
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InstitucionEducativa')
                BEGIN
                    CREATE TABLE InstitucionEducativa (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        NombreInstitucion NVARCHAR(200) NOT NULL,
                        Direccion NVARCHAR(250) NULL,
                        UrlLogo NVARCHAR(MAX) NULL, -- Admite Base64 o URL de imagen
                        FechaActualizacion DATETIME2 NOT NULL DEFAULT (DATEADD(HOUR, -5, GETUTCDATE())),
                        UsuarioActualizacion VARCHAR(100) NOT NULL
                    );

                    -- Insertar registro semilla inicial
                    INSERT INTO InstitucionEducativa (
                        NombreInstitucion, Direccion, UrlLogo, UsuarioActualizacion
                    ) VALUES (
                        'I.E. INSTITUCIÓN EDUCATIVA MODELO', 'Av. Principal N° 123 - Lima', NULL, 'SISTEMA_ADMIN'
                    );
                END
            ");

            // 2. SP: Obtener Configuración Institucional (Siempre trae el único registro)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_InstitucionEducativa_Obtener
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP 1
                        Id,
                        NombreInstitucion,
                        Direccion,
                        UrlLogo,
                        FechaActualizacion,
                        UsuarioActualizacion
                    FROM InstitucionEducativa;
                END
            ");

            // 3. SP: Actualizar Configuración Institucional
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_InstitucionEducativa_Guardar
                    @NombreInstitucion NVARCHAR(200),
                    @Direccion NVARCHAR(250) = NULL,
                    @UrlLogo NVARCHAR(MAX) = NULL,
                    @UsuarioActualizacion VARCHAR(100)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (SELECT 1 FROM InstitucionEducativa)
                    BEGIN
                        UPDATE TOP (1) InstitucionEducativa
                        SET NombreInstitucion = @NombreInstitucion,
                            Direccion = @Direccion,
                            UrlLogo = @UrlLogo,
                            FechaActualizacion = DATEADD(HOUR, -5, GETUTCDATE()),
                            UsuarioActualizacion = @UsuarioActualizacion;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO InstitucionEducativa (
                            NombreInstitucion, Direccion, UrlLogo, UsuarioActualizacion
                        ) VALUES (
                            @NombreInstitucion, @Direccion, @UrlLogo, @UsuarioActualizacion
                        );
                    END

                    SELECT 1;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_InstitucionEducativa_Guardar;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_InstitucionEducativa_Obtener;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS InstitucionEducativa;");
        }
    }
}
