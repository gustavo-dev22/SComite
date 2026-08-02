using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpAuditoriaResumenGeneralCajas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Auditoria_ResumenGeneralCajas
                    @AnioLectivo INT,
                    @Nivel VARCHAR(50) = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    WITH IngresosPorAula AS (
                        -- Suma de Cuotas / Pagos Validados
                        SELECT 
                            a.Id AS AulaId,
                            ISNULL(SUM(p.MontoPagado), 0) AS TotalCuotas
                        FROM Aulas a
                        LEFT JOIN CuotasAula c ON c.AulaId = a.Id AND c.Estado = 1
                        LEFT JOIN PagosCuota p ON p.CuotaId = c.Id AND p.EstadoPago = 'APROBADO' AND p.Estado = 1
                        WHERE a.Estado = 1
                        GROUP BY a.Id

                        UNION ALL

                        -- Suma de Donaciones Aprobadas
                        SELECT 
                            a.Id AS AulaId,
                            ISNULL(SUM(d.Monto), 0) AS TotalDonaciones
                        FROM Aulas a
                        LEFT JOIN DonacionesAula d ON d.AulaId = a.Id AND d.Estado = 1
                        WHERE a.Estado = 1
                        GROUP BY a.Id
                    ),
                    EgresosPorAula AS (
                        -- Suma de Gastos / Egresos Registrados
                        SELECT 
                            a.Id AS AulaId,
                            ISNULL(SUM(g.Monto), 0) AS TotalGastos
                        FROM Aulas a
                        LEFT JOIN GastosAula g ON g.AulaId = a.Id AND g.Estado = 1
                        WHERE a.Estado = 1
                        GROUP BY a.Id
                    ),
                    TotalesIngresosConsolidados AS (
                        SELECT AulaId, SUM(TotalCuotas) AS TotalIngresos
                        FROM IngresosPorAula
                        GROUP BY AulaId
                    ),
                    TotalesEgresosConsolidados AS (
                        SELECT AulaId, SUM(TotalGastos) AS TotalGastos
                        FROM EgresosPorAula
                        GROUP BY AulaId
                    )
                    SELECT 
                        a.Id AS AulaId,
                        a.Nivel,
                        a.Grado,
                        a.Seccion,
                        (a.Nivel + ' - ' + CAST(a.Grado AS VARCHAR(5)) + '° ""' + a.Seccion + '""') AS NombreAula,
                        ISNULL(i.TotalIngresos, 0) AS TotalIngresos,
                        ISNULL(e.TotalGastos, 0) AS TotalEgresos,
                        (ISNULL(i.TotalIngresos, 0) - ISNULL(e.TotalGastos, 0)) AS SaldoNeto,
                        CASE 
                            WHEN (ISNULL(i.TotalIngresos, 0) - ISNULL(e.TotalGastos, 0)) < 0 THEN 'ALERTA_ROJO'
                            WHEN ISNULL(i.TotalIngresos, 0) = 0 AND ISNULL(e.TotalGastos, 0) = 0 THEN 'SIN_MOVIMIENTO'
                            ELSE 'AL_DIA'
                        END AS EstadoFinanciero
                    FROM Aulas a
                    JOIN PeriodosLectivos p ON a.PeriodoLectivoId = p.Id
                    LEFT JOIN TotalesIngresosConsolidados i ON i.AulaId = a.Id
                    LEFT JOIN TotalesEgresosConsolidados e ON e.AulaId = a.Id
                    WHERE p.Anio = @AnioLectivo
                      AND a.Estado = 1
                      AND (@Nivel IS NULL OR @Nivel = '' OR LOWER(a.Nivel) = LOWER(@Nivel))
                    ORDER BY a.Nivel ASC, a.Grado ASC, a.Seccion ASC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Auditoria_ResumenGeneralCajas;");
        }
    }
}
