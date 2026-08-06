using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fix6_sp_Apoderado_ObtenerTransparenciaBalanceAula : Migration
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
                    
                    SET LANGUAGE Spanish;

                    -- 1. Resumen General Acumulado del Año (Cuotas Pagadas + Donaciones)
                    SELECT 
                        (
                            ISNULL((
                                SELECT SUM(d.MontoPagado) 
                                FROM CuotaDetalleEstudiante d
                                INNER JOIN Cuotas c ON d.CuotaId = c.Id
                                WHERE c.AulaId = @AulaId 
                                  AND UPPER(TRIM(d.EstadoPago)) IN ('PAGADO', 'VALIDADO', 'COMPLETO', 'APROBADO')
                                  AND d.MontoPagado > 0
                                  AND YEAR(ISNULL(d.FechaUltimoPago, ISNULL(c.FechaVencimiento, c.FechaCreacion))) = @Anio
                            ), 0)
                            +
                            ISNULL((
                                SELECT SUM(don.Monto)
                                FROM DonacionesComite don
                                WHERE don.AulaId = @AulaId
                                  AND don.Monto > 0
                                  AND YEAR(ISNULL(don.FechaDonacion, don.FechaRegistro)) = @Anio
                            ), 0)
                        ) AS TotalIngresos,
                        ISNULL((
                            SELECT SUM(g.Monto) 
                            FROM GastosComite g 
                            WHERE g.AulaId = @AulaId
                              AND YEAR(g.FechaGasto) = @Anio
                        ), 0) AS TotalEgresos;

                    -- 2. Balance Detallado por Mes (Cuotas + Donaciones vs Egresos)
                    WITH MovimientosMensuales AS (
                        -- A) Ingresos por Cuotas
                        SELECT 
                            MONTH(ISNULL(d.FechaUltimoPago, ISNULL(c.FechaVencimiento, c.FechaCreacion))) AS MesNum,
                            SUM(d.MontoPagado) AS Ingresos,
                            0.00 AS Egresos
                        FROM CuotaDetalleEstudiante d
                        INNER JOIN Cuotas c ON d.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND UPPER(TRIM(d.EstadoPago)) IN ('PAGADO', 'VALIDADO', 'COMPLETO', 'APROBADO')
                          AND d.MontoPagado > 0
                          AND YEAR(ISNULL(d.FechaUltimoPago, ISNULL(c.FechaVencimiento, c.FechaCreacion))) = @Anio
                        GROUP BY MONTH(ISNULL(d.FechaUltimoPago, ISNULL(c.FechaVencimiento, c.FechaCreacion)))

                        UNION ALL

                        -- B) Ingresos por Donaciones
                        SELECT 
                            MONTH(ISNULL(don.FechaDonacion, don.FechaRegistro)) AS MesNum,
                            SUM(don.Monto) AS Ingresos,
                            0.00 AS Egresos
                        FROM DonacionesComite don
                        WHERE don.AulaId = @AulaId
                          AND don.Monto > 0
                          AND YEAR(ISNULL(don.FechaDonacion, don.FechaRegistro)) = @Anio
                        GROUP BY MONTH(ISNULL(don.FechaDonacion, don.FechaRegistro))

                        UNION ALL

                        -- C) Egresos por Gastos
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
                        UPPER(LEFT(DATENAME(MONTH, DATEFROMPARTS(@Anio, MesNum, 1)), 1)) + 
                        LOWER(SUBSTRING(DATENAME(MONTH, DATEFROMPARTS(@Anio, MesNum, 1)), 2, 20)) AS NombreMes,
                        SUM(Ingresos) AS TotalIngresosMes,
                        SUM(Egresos) AS TotalEgresosMes,
                        (SUM(Ingresos) - SUM(Egresos)) AS SaldoMes
                    FROM MovimientosMensuales
                    GROUP BY MesNum
                    ORDER BY MesNum DESC;

                    -- 3. Listado Completo de Egresos
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

        }
    }
}
