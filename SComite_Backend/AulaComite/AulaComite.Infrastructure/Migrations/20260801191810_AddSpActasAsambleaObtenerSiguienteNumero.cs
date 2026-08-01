using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpActasAsambleaObtenerSiguienteNumero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_ActasAsamblea_ObtenerSiguienteNumero
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @MaxNumero INT = 0;
                    DECLARE @Prefijo VARCHAR(10) = 'ACTA-';
                    DECLARE @AnioStr VARCHAR(5) = CAST(@AnioLectivo AS VARCHAR(4));

                    -- Extraer el número máximo correlativo registrado en el año (incluso eliminadas)
                    SELECT @MaxNumero = ISNULL(MAX(
                        CAST(
                            SUBSTRING(
                                NumeroActa, 
                                LEN(@Prefijo) + 1, 
                                CHARINDEX('-', NumeroActa, LEN(@Prefijo) + 1) - (LEN(@Prefijo) + 1)
                            ) AS INT
                        )
                    ), 0)
                    FROM ActasAsambleaComite
                    WHERE AulaId = @AulaId 
                      AND YEAR(FechaReunion) = @AnioLectivo
                      AND NumeroActa LIKE 'ACTA-%-' + @AnioStr;

                    -- Incrementar el correlativo
                    DECLARE @SiguienteNumero INT = @MaxNumero + 1;

                    -- Formatear a 3 dígitos (ej: ACTA-004-2026)
                    SELECT @Prefijo + RIGHT('000' + CAST(@SiguienteNumero AS VARCHAR(10)), 3) + '-' + @AnioStr AS SiguienteNumeroActa;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ActasAsamblea_ObtenerSiguienteNumero;");
        }
    }
}
