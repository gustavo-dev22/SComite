using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modulo_Padron_Estudiantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear Tabla Estudiantes
            migrationBuilder.CreateTable(
                name: "Estudiantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AulaId = table.Column<int>(type: "int", nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "DNI"),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApellidoPaterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApellidoMaterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsuarioIdApoderadoSasi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NombreApoderado = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TelefonoApoderado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estudiantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Estudiantes_Aulas_AulaId",
                        column: x => x.AulaId,
                        principalTable: "Aulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_AulaId",
                table: "Estudiantes",
                column: "AulaId");

            // 2. STORED PROCEDURES

            // SP: Obtener Estudiantes por Aula
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Estudiantes_ObtenerPorAula
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT e.Id, e.AulaId, e.TipoDocumento, e.NumeroDocumento, 
                           e.Nombres, e.ApellidoPaterno, e.ApellidoMaterno,
                           (e.ApellidoPaterno + ' ' + e.ApellidoMaterno + ', ' + e.Nombres) AS NombreCompleto,
                           e.UsuarioIdApoderadoSasi, e.NombreApoderado, e.TelefonoApoderado, 
                           e.Estado, e.FechaRegistro
                    FROM Estudiantes e
                    WHERE e.AulaId = @AulaId
                    ORDER BY e.ApellidoPaterno, e.ApellidoMaterno, e.Nombres;
                END
            ");

            // SP: Crear Estudiante
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Estudiantes_Crear
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

                    INSERT INTO Estudiantes (AulaId, TipoDocumento, NumeroDocumento, Nombres, ApellidoPaterno, ApellidoMaterno, UsuarioIdApoderadoSasi, NombreApoderado, TelefonoApoderado, Estado, FechaRegistro)
                    VALUES (@AulaId, @TipoDocumento, @NumeroDocumento, UPPER(@Nombres), UPPER(@ApellidoPaterno), UPPER(@ApellidoMaterno), @UsuarioIdApoderadoSasi, @NombreApoderado, @TelefonoApoderado, 1, GETUTCDATE());

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ");

            // SP: Actualizar Estudiante
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Estudiantes_Actualizar
                    @Id INT,
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

                    UPDATE Estudiantes
                    SET AulaId = @AulaId,
                        TipoDocumento = @TipoDocumento,
                        NumeroDocumento = @NumeroDocumento,
                        Nombres = UPPER(@Nombres),
                        ApellidoPaterno = UPPER(@ApellidoPaterno),
                        ApellidoMaterno = UPPER(@ApellidoMaterno),
                        UsuarioIdApoderadoSasi = @UsuarioIdApoderadoSasi,
                        NombreApoderado = @NombreApoderado,
                        TelefonoApoderado = @TelefonoApoderado
                    WHERE Id = @Id;
                END
            ");

            // SP: Eliminar Lógico (Desactivar)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Estudiantes_EliminarLogico
                    @Id INT
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    UPDATE Estudiantes
                    SET Estado = 0
                    WHERE Id = @Id;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Estudiantes_ObtenerPorAula");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Estudiantes_Crear");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Estudiantes_Actualizar");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Estudiantes_EliminarLogico");
            migrationBuilder.DropTable(name: "Estudiantes");
        }
    }
}
