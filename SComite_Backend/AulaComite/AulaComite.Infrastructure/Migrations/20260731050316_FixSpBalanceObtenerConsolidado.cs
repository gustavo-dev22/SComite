using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSpBalanceObtenerConsolidado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Balance_ObtenerConsolidado]
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL -- NULL o 0 = Acumulado Todo el Año, 3 = Marzo... 12 = Diciembre
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
                        -- 🚀 CASO A: TODO EL AÑO (ACUMULADO GLOBAL DEL AÑO LECTIVO)
                        SET @SaldoAnteriorArrastrado = 0.00;

                        SELECT @IngresosMensuales = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        INNER JOIN Aulas au ON c.AulaId = au.Id
                        INNER JOIN PeriodosLectivos p ON au.PeriodoId = p.Id
                        WHERE c.AulaId = @AulaId 
                          AND c.TipoCuota = 'RECURRENTE_MENSUAL'
                          AND p.Anio = @AnioLectivo;

                        SELECT @IngresosExtraordinarios = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        INNER JOIN Aulas au ON c.AulaId = au.Id
                        INNER JOIN PeriodosLectivos p ON au.PeriodoId = p.Id
                        WHERE c.AulaId = @AulaId 
                          AND c.TipoCuota = 'EXTRAORDINARIA'
                          AND p.Anio = @AnioLectivo;

                        SELECT @TotalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND YEAR(FechaGasto) = @AnioLectivo;
                    END
                    ELSE
                    BEGIN
                        -- 🚀 CASO B: CORTE DE MES ESPECÍFICO (CON ARRASTRE DE SALDO)

                        -- 1. Saldo Arrastrado de Meses Anteriores (< @Mes)
                        DECLARE @IngresosAnteriores DECIMAL(10,2) = 0.00;
                        DECLARE @EgresosAnteriores DECIMAL(10,2) = 0.00;

                        SELECT @IngresosAnteriores = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND MONTH(c.FechaVencimiento) < @Mes
                          AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        SELECT @EgresosAnteriores = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND MONTH(FechaGasto) < @Mes
                          AND YEAR(FechaGasto) = @AnioLectivo;

                        SET @SaldoAnteriorArrastrado = (@IngresosAnteriores - @EgresosAnteriores);

                        -- 2. Ingresos Recaudados Correspondientes al Mes Seleccionado (@Mes)
                        SELECT @IngresosMensuales = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND c.TipoCuota = 'RECURRENTE_MENSUAL'
                          AND MONTH(c.FechaVencimiento) = @Mes
                          AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        SELECT @IngresosExtraordinarios = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND c.TipoCuota = 'EXTRAORDINARIA'
                          AND MONTH(c.FechaVencimiento) = @Mes
                          AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        -- 3. Egresos Ejecutados en el Mes Seleccionado (@Mes)
                        SELECT @TotalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND MONTH(FechaGasto) = @Mes
                          AND YEAR(FechaGasto) = @AnioLectivo;
                    END

                    -- 4. Métricas de Morosidad y Cumplimiento
                    SELECT @TotalPorCobrar = ISNULL(SUM(cd.MontoAsignado - cd.MontoPagado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId 
                      AND cd.EstadoPago <> 'COMPLETO'
                      AND (@Mes IS NULL OR @Mes = 0 OR MONTH(c.FechaVencimiento) <= @Mes);

                    SELECT @TotalAsignado = ISNULL(SUM(cd.MontoAsignado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId
                      AND (@Mes IS NULL OR @Mes = 0 OR MONTH(c.FechaVencimiento) <= @Mes);

                    IF @TotalAsignado > 0
                        SET @PorcentajeCumplimiento = (((@IngresosMensuales + @IngresosExtraordinarios + CASE WHEN @Mes > 0 THEN @SaldoAnteriorArrastrado ELSE 0 END)) / @TotalAsignado) * 100;

                    -- Retornar Consolidado Financiero
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
