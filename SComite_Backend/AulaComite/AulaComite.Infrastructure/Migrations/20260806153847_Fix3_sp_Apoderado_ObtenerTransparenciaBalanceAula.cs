using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fix3_sp_Apoderado_ObtenerTransparenciaBalanceAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Apoderado_ObtenerTransparenciaBalanceAula
                    @AulaId INT,
                    @Anio INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    -- 🚀 Forzar idioma en español para DATENAME
                    SET LANGUAGE Spanish;

                    -- 1. Resumen General Acumulado del Año Seleccionado
                    SELECT 
                        ISNULL((
                            SELECT SUM(d.MontoPagado) 
                            FROM CuotaDetalleEstudiante d
                            INNER JOIN Cuotas c ON d.CuotaId = c.Id
                            WHERE c.AulaId = @AulaId 
                              AND d.EstadoPago IN ('PAGADO', 'VALIDADO')
                              AND YEAR(ISNULL(d.FechaUltimoPago, c.FechaVencimiento)) = @Anio
                        ), 0) AS TotalIngresos,
                        ISNULL((
                            SELECT SUM(g.Monto) 
                            FROM GastosComite g 
                            WHERE g.AulaId = @AulaId
                              AND YEAR(g.FechaGasto) = @Anio
                        ), 0) AS TotalEgresos;

                    -- 2. Balance Detallado por Mes (Ingresos vs Egresos)
                    WITH MovimientosMensuales AS (
                        -- Ingresos: Si FechaUltimoPago es NULL toma FechaVencimiento de la Cuota
                        SELECT 
                            MONTH(ISNULL(d.FechaUltimoPago, c.FechaVencimiento)) AS MesNum,
                            SUM(d.MontoPagado) AS Ingresos,
                            0.00 AS Egresos
                        FROM CuotaDetalleEstudiante d
                        INNER JOIN Cuotas c ON d.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND d.EstadoPago IN ('PAGADO', 'VALIDADO')
                          AND YEAR(ISNULL(d.FechaUltimoPago, c.FechaVencimiento)) = @Anio
                        GROUP BY MONTH(ISNULL(d.FechaUltimoPago, c.FechaVencimiento))

                        UNION ALL

                        -- Egresos agrupados por mes de gasto
                        SELECT 
                            MONTH(g.FechaGasto) AS MesNum,
                            0.00 AS Ingresos,
                            SUM(g.Monto) AS Egresos
                        FROM GastosComite g
                        WHERE g.AulaId = @AulaId
                          AND YEAR(g.FechaGasto) = @Anio
                        GROUP BY MONTH(g.FechaGasto)
                    )
                    SELECT 
                        @Anio AS Anio,
                        MesNum,
                        -- Retorna el mes en español con la primera letra en mayúscula (ej: 'Agosto')
                        UPPER(LEFT(DATENAME(MONTH, DATEFROMPARTS(@Anio, MesNum, 1)), 1)) + 
                        LOWER(SUBSTRING(DATENAME(MONTH, DATEFROMPARTS(@Anio, MesNum, 1)), 2, 20)) AS NombreMes,
                        SUM(Ingresos) AS TotalIngresosMes,
                        SUM(Egresos) AS TotalEgresosMes,
                        (SUM(Ingresos) - SUM(Egresos)) AS SaldoMes
                    FROM MovimientosMensuales
                    GROUP BY MesNum
                    ORDER BY MesNum DESC;

                    -- 3. Listado Completo de Egresos del Año
                    SELECT 
                        g.Id,
                        g.FechaGasto,
                        g.Concepto,
                        g.Categoria,
                        g.Monto,
                        g.Proveedor,
                        g.TipoComprobante,
                        g.NumeroComprobante,
                        g.UrlComprobante
                    FROM GastosComite g
                    WHERE g.AulaId = @AulaId
                      AND YEAR(g.FechaGasto) = @Anio
                    ORDER BY g.FechaGasto DESC;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_Apoderado_ObtenerTransparenciaBalanceAula')
                    DROP PROCEDURE sp_Apoderado_ObtenerTransparenciaBalanceAula;
            ");
        }
    }
}
