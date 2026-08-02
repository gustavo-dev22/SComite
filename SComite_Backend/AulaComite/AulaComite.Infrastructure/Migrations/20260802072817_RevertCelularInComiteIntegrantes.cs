using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RevertCelularInComiteIntegrantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Eliminar la columna Celular de ComiteIntegrantes
            migrationBuilder.DropColumn(
                name: "Celular",
                table: "ComiteIntegrantes");

            // 2. Restaurar el Stored Procedure sp_Comite_AsignarIntegrante a su versión original
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Comite_AsignarIntegrante
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

                    -- Insertar la nueva asignación
                    INSERT INTO ComiteIntegrantes (AulaId, UsuarioIdSasi, NombreCompleto, Email, Cargo, Estado, FechaAsignacion)
                    VALUES (@AulaId, @UsuarioIdSasi, @NombreCompleto, @Email, @Cargo, 1, DATEADD(HOUR, -5, GETUTCDATE()));

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
