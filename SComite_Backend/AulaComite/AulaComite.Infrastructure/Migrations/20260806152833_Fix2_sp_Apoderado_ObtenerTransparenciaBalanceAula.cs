using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fix2_sp_Apoderado_ObtenerTransparenciaBalanceAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Apoderado_ObtenerTransparenciaBalanceAula
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- 1. Resumen General Acumulado
                    SELECT 
                        ISNULL((
                            SELECT SUM(d.MontoPagado) 
                            FROM CuotaDetalleEstudiante d
                            INNER JOIN Cuotas c ON d.CuotaId = c.Id
                            WHERE c.AulaId = @AulaId AND d.EstadoPago IN ('PAGADO', 'VALIDADO')
                        ), 0) AS TotalIngresos,
                        ISNULL((
                            SELECT SUM(g.Monto) 
                            FROM GastosComite g 
                            WHERE g.AulaId = @AulaId
                        ), 0) AS TotalEgresos;

                    -- 2. Balance Detallado por Mes (Union de Ingresos y Egresos)
                    WITH MovimientosMensuales AS (
                        -- Ingresos agrupados por mes de pago
                        SELECT 
                            YEAR(d.FechaUltimoPago) AS Anio,
                            MONTH(d.FechaUltimoPago) AS MesNum,
                            SUM(d.MontoPagado) AS Ingresos,
                            0.00 AS Egresos
                        FROM CuotaDetalleEstudiante d
                        INNER JOIN Cuotas c ON d.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND d.EstadoPago IN ('PAGADO', 'VALIDADO')
                          AND d.FechaUltimoPago IS NOT NULL
                        GROUP BY YEAR(d.FechaUltimoPago), MONTH(d.FechaUltimoPago)

                        UNION ALL

                        -- Egresos agrupados por mes de gasto
                        SELECT 
                            YEAR(g.FechaGasto) AS Anio,
                            MONTH(g.FechaGasto) AS MesNum,
                            0.00 AS Ingresos,
                            SUM(g.Monto) AS Egresos
                        FROM GastosComite g
                        WHERE g.AulaId = @AulaId
                        GROUP BY YEAR(g.FechaGasto), MONTH(g.FechaGasto)
                    )
                    SELECT 
                        Anio,
                        MesNum,
                        DATENAME(MONTH, DATEFROMPARTS(Anio, MesNum, 1)) AS NombreMes,
                        SUM(Ingresos) AS TotalIngresosMes,
                        SUM(Egresos) AS TotalEgresosMes,
                        (SUM(Ingresos) - SUM(Egresos)) AS SaldoMes
                    FROM MovimientosMensuales
                    GROUP BY Anio, MesNum
                    ORDER BY Anio DESC, MesNum DESC;

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
