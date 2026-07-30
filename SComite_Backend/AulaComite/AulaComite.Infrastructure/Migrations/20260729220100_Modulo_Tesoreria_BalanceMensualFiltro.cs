using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modulo_Tesoreria_BalanceMensualFiltro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. SP: Consolidado con Filtro por Mes y Arrastre Histórico
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Balance_ObtenerConsolidado
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL -- NULL = Acumulado Todo el Año, 3 = Marzo... 12 = Diciembre
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @SaldoAnteriorArrastrado DECIMAL(10,2) = 0.00;
                    DECLARE @IngresosMensuales DECIMAL(10,2) = 0.00;
                    DECLARE @IngresosExtraordinarios DECIMAL(10,2) = 0.00;
                    DECLARE @TotalEgresos DECIMAL(10,2) = 0.00;
                    DECLARE @TotalPorCobrar DECIMAL(10,2) = 0.00;
                    DECLARE @TotalAsignado DECIMAL(10,2) = 0.00;
                    DECLARE @PorcentajeCumplimiento DECIMAL(5,2) = 0.00;

                    IF @Mes IS NULL OR @Mes = 0
                    BEGIN
                        -- 🚀 CASO A: TODO EL AÑO (SIN FILTRO DE MES)
                        SET @SaldoAnteriorArrastrado = 0.00;

                        SELECT @IngresosMensuales = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND c.TipoCuota = 'RECURRENTE_MENSUAL';

                        SELECT @IngresosExtraordinarios = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND c.TipoCuota = 'EXTRAORDINARIA';

                        SELECT @TotalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId;
                    END
                    ELSE
                    BEGIN
                        -- 🚀 CASO B: MES ESPECÍFICO (CORTE DE MES CON ARRASTRE DE CAJA)
                        
                        -- 1. Saldo Arrastrado del Mes Anterior
                        DECLARE @IngresosAnteriores DECIMAL(10,2) = 0.00;
                        DECLARE @EgresosAnteriores DECIMAL(10,2) = 0.00;

                        SELECT @IngresosAnteriores = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND cd.FechaUltimoPago IS NOT NULL
                          AND MONTH(cd.FechaUltimoPago) < @Mes
                          AND YEAR(cd.FechaUltimoPago) = @AnioLectivo;

                        SELECT @EgresosAnteriores = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND MONTH(FechaGasto) < @Mes
                          AND YEAR(FechaGasto) = @AnioLectivo;

                        SET @SaldoAnteriorArrastrado = (@IngresosAnteriores - @EgresosAnteriores);

                        -- 2. Ingresos del Mes Específico
                        SELECT @IngresosMensuales = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND c.TipoCuota = 'RECURRENTE_MENSUAL'
                          AND cd.FechaUltimoPago IS NOT NULL
                          AND MONTH(cd.FechaUltimoPago) = @Mes
                          AND YEAR(cd.FechaUltimoPago) = @AnioLectivo;

                        SELECT @IngresosExtraordinarios = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND c.TipoCuota = 'EXTRAORDINARIA'
                          AND cd.FechaUltimoPago IS NOT NULL
                          AND MONTH(cd.FechaUltimoPago) = @Mes
                          AND YEAR(cd.FechaUltimoPago) = @AnioLectivo;

                        -- 3. Egresos del Mes Específico
                        SELECT @TotalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND MONTH(FechaGasto) = @Mes
                          AND YEAR(FechaGasto) = @AnioLectivo;
                    END

                    -- 4. Morosidad y Pendientes Globales del Aula
                    SELECT @TotalPorCobrar = ISNULL(SUM(cd.MontoAsignado - cd.MontoPagado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId AND cd.EstadoPago <> 'COMPLETO';

                    SELECT @TotalAsignado = ISNULL(SUM(cd.MontoAsignado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId;

                    IF @TotalAsignado > 0
                        SET @PorcentajeCumplimiento = (((@IngresosMensuales + @IngresosExtraordinarios + CASE WHEN @Mes > 0 THEN @SaldoAnteriorArrastrado ELSE 0 END)) / @TotalAsignado) * 100;

                    -- Retornar Consolidado
                    SELECT 
                        @SaldoAnteriorArrastrado AS SaldoAnteriorArrastrado,
                        @IngresosMensuales AS IngresosMensuales,
                        @IngresosExtraordinarios AS IngresosExtraordinarios,
                        (@IngresosMensuales + @IngresosExtraordinarios) AS TotalIngresosMes,
                        @TotalEgresos AS TotalEgresosMes,
                        (@SaldoAnteriorArrastrado + @IngresosMensuales + @IngresosExtraordinarios - @TotalEgresos) AS SaldoNetoEnCaja,
                        @TotalPorCobrar AS TotalPorCobrar,
                        @PorcentajeCumplimiento AS PorcentajeCumplimiento;
                END
            ");

            // 2. SP: Gastos por Categoría con Filtro por Mes
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Balance_ObtenerGastosPorCategoria
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        Categoria,
                        ISNULL(SUM(Monto), 0.00) AS TotalMonto,
                        COUNT(Id) AS CantidadRegistros
                    FROM GastosComite
                    WHERE AulaId = @AulaId
                      AND (@Mes IS NULL OR @Mes = 0 OR (MONTH(FechaGasto) = @Mes AND YEAR(FechaGasto) = @AnioLectivo))
                    GROUP BY Categoria
                    ORDER BY TotalMonto DESC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Balance_ObtenerGastosPorCategoria");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Balance_ObtenerConsolidado");
        }
    }
}
