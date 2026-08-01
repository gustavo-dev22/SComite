using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSpListarActividades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Actividades_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        a.Id,
                        a.AulaId,
                        a.NombreActividad,
                        a.Descripcion,
                        a.FechaProgramada,
                        a.MontoPresupuestado,
                        a.CuotaSugeridaPorAlumno,
                        a.Estado,
                        a.FechaRegistro
                    FROM ActividadesComite a
                    INNER JOIN Aulas au ON a.AulaId = au.Id
                    INNER JOIN PeriodosLectivos p ON au.Id = p.Id
                    WHERE a.AulaId = @AulaId
                      AND p.Anio = @AnioLectivo
                    ORDER BY a.FechaProgramada ASC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // En el Down restauramos el estado anterior por seguridad
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Actividades_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        a.Id,
                        a.AulaId,
                        a.NombreActividad,
                        a.Descripcion,
                        a.FechaProgramada,
                        a.MontoPresupuestado,
                        a.CuotaSugeridaPorAlumno,
                        a.Estado,
                        a.FechaRegistro
                    FROM ActividadesComite a
                    WHERE a.AulaId = @AulaId
                    ORDER BY a.FechaProgramada ASC;
                END
            ");
        }
    }
}
