using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial_Periodos_Aulas_SPs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Aulas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodoId = table.Column<int>(type: "int", nullable: false),
                    Nivel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Grado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Seccion = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NombreDisplay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aulas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeriodosLectivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EsActivo = table.Column<bool>(type: "bit", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosLectivos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosLectivos_Anio",
                table: "PeriodosLectivos",
                column: "Anio",
                unique: true);

            // SP: Obtener Aulas
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_Aulas_ObtenerTodas
                    @PeriodoId INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT a.Id, a.PeriodoId, a.Nivel, a.Grado, a.Seccion, 
                           (a.Nivel + ' - ' + a.Grado + ' ' + a.Seccion) AS NombreDisplay,
                           a.Estado, p.Anio AS AnioPeriodo
                    FROM Aulas a
                    INNER JOIN PeriodosLectivos p ON a.PeriodoId = p.Id
                    WHERE (@PeriodoId IS NULL OR a.PeriodoId = @PeriodoId)
                    ORDER BY p.Anio DESC, a.Nivel, a.Grado, a.Seccion;
                END
            ");

            // SP: Obtener Periodos
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_Periodos_ObtenerTodos
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT Id, Anio, Nombre, EsActivo, FechaInicio, FechaFin
                    FROM PeriodosLectivos
                    ORDER BY Anio DESC;
                END
            ");

            // SP: Crear Aula
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_Aulas_Crear
                    @PeriodoId INT,
                    @Nivel VARCHAR(30),
                    @Grado VARCHAR(50),
                    @Seccion VARCHAR(10)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    INSERT INTO Aulas (PeriodoId, Nivel, Grado, Seccion, Estado)
                    VALUES (@PeriodoId, @Nivel, @Grado, @Seccion, 1);
                    
                    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
                END
            ");

            // SP: Actualizar Estado Aula
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_Aulas_ActualizarEstado
                    @Id INT,
                    @Estado BIT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE Aulas 
                    SET Estado = @Estado 
                    WHERE Id = @Id;
                END
            ");

            // 3. SEED DATA (Datos iniciales)
            migrationBuilder.Sql(@"
                INSERT INTO PeriodosLectivos (Anio, Nombre, EsActivo, FechaInicio, FechaFin) VALUES
                (2025, 'Año Lectivo 2025', 0, '2025-03-01', '2025-12-20'),
                (2026, 'Año Lectivo 2026', 1, '2026-03-01', '2026-12-20'),
                (2027, 'Año Lectivo 2027', 1, '2027-03-01', '2027-12-20');

                INSERT INTO Aulas (PeriodoId, Nivel, Grado, Seccion, Estado) VALUES
                (2, 'INICIAL', '5 AÑOS', 'A', 1),
                (2, 'PRIMARIA', 'PRIMER GRADO', 'B', 1),
                (2, 'PRIMARIA', 'SEGUNDO GRADO', 'A', 1);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminación de Stored Procedures
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Aulas_ObtenerTodas");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Periodos_ObtenerTodos");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Aulas_Crear");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Aulas_ActualizarEstado");

            migrationBuilder.DropTable(
                name: "Aulas");

            migrationBuilder.DropTable(
                name: "PeriodosLectivos");
        }
    }
}
