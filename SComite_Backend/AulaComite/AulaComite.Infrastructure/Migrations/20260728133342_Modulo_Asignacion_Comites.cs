using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modulo_Asignacion_Comites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear Tabla ComiteIntegrantes
            migrationBuilder.CreateTable(
                name: "ComiteIntegrantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AulaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioIdSasi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false), // PRESIDENTE, TESORERO, SECRETARIO, VOCAL
                    Estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComiteIntegrantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComiteIntegrantes_Aulas_AulaId",
                        column: x => x.AulaId,
                        principalTable: "Aulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComiteIntegrantes_AulaId",
                table: "ComiteIntegrantes",
                column: "AulaId");

            // 2. STORED PROCEDURES

            // SP: Obtener integrantes del comité por Aula
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Comite_ObtenerPorAula
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT Id, AulaId, UsuarioIdSasi, NombreCompleto, Email, Cargo, Estado, FechaAsignacion
                    FROM ComiteIntegrantes
                    WHERE AulaId = @AulaId AND Estado = 1
                    ORDER BY 
                        CASE Cargo 
                            WHEN 'PRESIDENTE' THEN 1 
                            WHEN 'TESORERO' THEN 2 
                            WHEN 'SECRETARIO' THEN 3 
                            WHEN 'VOCAL' THEN 4 
                            ELSE 5 
                        END;
                END
            ");

            // SP: Asignar o Reemplazar integrante de comité
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
                    VALUES (@AulaId, @UsuarioIdSasi, @NombreCompleto, @Email, @Cargo, 1, GETUTCDATE());

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ");

            // SP: Eliminar/Desactivar Integrante
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Comite_EliminarIntegrante
                    @Id INT
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    UPDATE ComiteIntegrantes
                    SET Estado = 0
                    WHERE Id = @Id;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Comite_ObtenerPorAula");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Comite_AsignarIntegrante");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Comite_EliminarIntegrante");
            migrationBuilder.DropTable(name: "ComiteIntegrantes");
        }
    }
}
