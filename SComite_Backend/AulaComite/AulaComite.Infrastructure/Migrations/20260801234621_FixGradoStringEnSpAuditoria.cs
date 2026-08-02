using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixGradoStringEnSpAuditoria : Migration
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

                    WITH IngresosCuotas AS (
                        SELECT 
                            a.Id AS AulaId,
                            ISNULL(SUM(d.MontoPagado), 0) AS TotalCuotas
                        FROM Aulas a
                        LEFT JOIN Cuotas c ON c.AulaId = a.Id AND c.Estado = 1
                        LEFT JOIN CuotaDetalleEstudiante d ON d.CuotaId = c.Id AND d.EstadoPago = 'APROBADO'
                        WHERE a.Estado = 1
                        GROUP BY a.Id
                    ),
                    IngresosDonaciones AS (
                        SELECT 
                            a.Id AS AulaId,
                            ISNULL(SUM(don.Monto), 0) AS TotalDonaciones
                        FROM Aulas a
                        LEFT JOIN DonacionesComite don ON don.AulaId = a.Id
                        WHERE a.Estado = 1
                        GROUP BY a.Id
                    ),
                    EgresosGastos AS (
                        SELECT 
                            a.Id AS AulaId,
                            ISNULL(SUM(g.Monto), 0) AS TotalGastos
                        FROM Aulas a
                        LEFT JOIN GastosComite g ON g.AulaId = a.Id
                        WHERE a.Estado = 1
                        GROUP BY a.Id
                    ),
                    TotalesConsolidados AS (
                        SELECT 
                            a.Id AS AulaId,
                            ISNULL(ic.TotalCuotas, 0) + ISNULL(id.TotalDonaciones, 0) AS TotalIngresos,
                            ISNULL(eg.TotalGastos, 0) AS TotalEgresos
                        FROM Aulas a
                        LEFT JOIN IngresosCuotas ic ON ic.AulaId = a.Id
                        LEFT JOIN IngresosDonaciones id ON id.AulaId = a.Id
                        LEFT JOIN EgresosGastos eg ON eg.AulaId = a.Id
                        WHERE a.Estado = 1
                    )
                    SELECT 
                        a.Id AS AulaId,
                        a.Nivel,
                        CAST(a.Grado AS VARCHAR(50)) AS Grado,
                        a.Seccion,
                        (a.Nivel + ' - ' + CAST(a.Grado AS VARCHAR(50)) + ' ""' + a.Seccion + '""') AS NombreAula,
                        t.TotalIngresos,
                        t.TotalEgresos,
                        (t.TotalIngresos - t.TotalEgresos) AS SaldoNeto,
                        CASE 
                            WHEN (t.TotalIngresos - t.TotalEgresos) < 0 THEN 'ALERTA_ROJO'
                            WHEN t.TotalIngresos = 0 AND t.TotalEgresos = 0 THEN 'SIN_MOVIMIENTO'
                            ELSE 'AL_DIA'
                        END AS EstadoFinanciero
                    FROM Aulas a
                    JOIN PeriodosLectivos p ON a.PeriodoId = p.Id
                    JOIN TotalesConsolidados t ON t.AulaId = a.Id
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

        }
    }
}
