using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFechaAsignacionComitePeru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Comite_AsignarIntegrante]
                    @AulaId INT,
                    @UsuarioIdSasi VARCHAR(100),
                    @NombreCompleto VARCHAR(150),
                    @Email VARCHAR(100),
                    @Cargo VARCHAR(30)
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    -- Desactivar asignación previa para el mismo cargo en esta aula
                    UPDATE ComiteIntegrantes 
                    SET Estado = 0 
                    WHERE AulaId = @AulaId AND Cargo = @Cargo AND Estado = 1;

                    -- 🚀 Insertar ajustando la hora a Perú (UTC-5)
                    INSERT INTO ComiteIntegrantes (
                        AulaId, 
                        UsuarioIdSasi, 
                        NombreCompleto, 
                        Email, 
                        Cargo, 
                        Estado, 
                        FechaAsignacion
                    )
                    VALUES (
                        @AulaId, 
                        @UsuarioIdSasi, 
                        @NombreCompleto, 
                        @Email, 
                        @Cargo, 
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
                CREATE OR ALTER PROCEDURE [dbo].[sp_Comite_AsignarIntegrante]
                    @AulaId INT,
                    @UsuarioIdSasi VARCHAR(100),
                    @NombreCompleto VARCHAR(150),
                    @Email VARCHAR(100),
                    @Cargo VARCHAR(30)
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    UPDATE ComiteIntegrantes 
                    SET Estado = 0 
                    WHERE AulaId = @AulaId AND Cargo = @Cargo AND Estado = 1;

                    INSERT INTO ComiteIntegrantes (
                        AulaId, 
                        UsuarioIdSasi, 
                        NombreCompleto, 
                        Email, 
                        Cargo, 
                        Estado, 
                        FechaAsignacion
                    )
                    VALUES (
                        @AulaId, 
                        @UsuarioIdSasi, 
                        @NombreCompleto, 
                        @Email, 
                        @Cargo, 
                        1, 
                        GETUTCDATE()
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ");
        }
    }
}
