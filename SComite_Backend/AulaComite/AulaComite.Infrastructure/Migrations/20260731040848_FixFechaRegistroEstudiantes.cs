using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFechaRegistroEstudiantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Estudiantes_Crear]
                    @AulaId INT,
                    @TipoDocumento VARCHAR(10),
                    @NumeroDocumento VARCHAR(15),
                    @Nombres VARCHAR(100),
                    @ApellidoPaterno VARCHAR(100),
                    @ApellidoMaterno VARCHAR(100),
                    @UsuarioIdApoderadoSasi VARCHAR(100) = NULL,
                    @NombreApoderado VARCHAR(150) = NULL,
                    @TelefonoApoderado VARCHAR(20) = NULL
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    -- 🚀 Insertar ajustando la fecha de registro a la hora exacta de Perú (UTC-5)
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
                    VALUES (
                        @AulaId, 
                        @TipoDocumento, 
                        @NumeroDocumento, 
                        UPPER(@Nombres), 
                        UPPER(@ApellidoPaterno), 
                        UPPER(@ApellidoMaterno), 
                        @UsuarioIdApoderadoSasi, 
                        @NombreApoderado, 
                        @TelefonoApoderado, 
                        1, 
                        DATEADD(HOUR, -5, GETUTCDATE())
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Estudiantes_Crear]
                    @AulaId INT,
                    @TipoDocumento VARCHAR(10),
                    @NumeroDocumento VARCHAR(15),
                    @Nombres VARCHAR(100),
                    @ApellidoPaterno VARCHAR(100),
                    @ApellidoMaterno VARCHAR(100),
                    @UsuarioIdApoderadoSasi VARCHAR(100) = NULL,
                    @NombreApoderado VARCHAR(150) = NULL,
                    @TelefonoApoderado VARCHAR(20) = NULL
                AS
                BEGIN
                    SET NOCOUNT OFF;

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
                    VALUES (
                        @AulaId, 
                        @TipoDocumento, 
                        @NumeroDocumento, 
                        UPPER(@Nombres), 
                        UPPER(@ApellidoPaterno), 
                        UPPER(@ApellidoMaterno), 
                        @UsuarioIdApoderadoSasi, 
                        @NombreApoderado, 
                        @TelefonoApoderado, 
                        1, 
                        GETUTCDATE()
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ");
        }
    }
}
