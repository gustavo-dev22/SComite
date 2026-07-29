using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modulo_CRUD_Periodos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SP: Crear Periodo Lectivo
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Periodos_Crear
                    @Anio INT,
                    @FechaInicio DATETIME2,
                    @FechaFin DATETIME2,
                    @EsActivo BIT
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    DECLARE @Nombre VARCHAR(100) = 'Año Lectivo ' + CAST(@Anio AS VARCHAR(4));

                    -- Si el nuevo periodo se marca como activo, desactivar los demás
                    IF @EsActivo = 1
                    BEGIN
                        UPDATE PeriodosLectivos SET EsActivo = 0;
                    END

                    INSERT INTO PeriodosLectivos (Anio, Nombre, EsActivo, FechaInicio, FechaFin)
                    VALUES (@Anio, @Nombre, @EsActivo, @FechaInicio, @FechaFin);

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ");

            // SP: Actualizar Periodo Lectivo
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Periodos_Actualizar
                    @Id INT,
                    @Anio INT,
                    @FechaInicio DATETIME2,
                    @FechaFin DATETIME2,
                    @EsActivo BIT
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    DECLARE @Nombre VARCHAR(100) = 'Año Lectivo ' + CAST(@Anio AS VARCHAR(4));

                    -- Si se activa este periodo, desactivar los demás
                    IF @EsActivo = 1
                    BEGIN
                        UPDATE PeriodosLectivos SET EsActivo = 0 WHERE Id <> @Id;
                    END

                    UPDATE PeriodosLectivos
                    SET Anio = @Anio,
                        Nombre = @Nombre,
                        EsActivo = @EsActivo,
                        FechaInicio = @FechaInicio,
                        FechaFin = @FechaFin
                    WHERE Id = @Id;
                END
            ");

            // SP: Cambiar Estado Activo (Soft Toggle)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Periodos_CambiarEstado
                    @Id INT,
                    @EsActivo BIT
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    IF @EsActivo = 1
                    BEGIN
                        UPDATE PeriodosLectivos SET EsActivo = 0;
                    END

                    UPDATE PeriodosLectivos
                    SET EsActivo = @EsActivo
                    WHERE Id = @Id;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Periodos_ObtenerTodos");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Periodos_Crear");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Periodos_Actualizar");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Periodos_CambiarEstado");
        }
    }
}
