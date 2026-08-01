using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SepararIngresosDonacionesEnBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Balance_ObtenerConsolidado]
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL -- NULL o 0 = Acumulado Todo el Año
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @SaldoAnteriorArrastrado DECIMAL(10,2) = 0.00;
                    DECLARE @IngresosMensuales DECIMAL(10,2) = 0.00;
                    DECLARE @IngresosExtraordinarios DECIMAL(10,2) = 0.00;
                    DECLARE @IngresosDonaciones DECIMAL(10,2) = 0.00;
                    DECLARE @TotalEgresos DECIMAL(10,2) = 0.00;
                    DECLARE @TotalPorCobrar DECIMAL(10,2) = 0.00;
                    DECLARE @TotalAsignado DECIMAL(10,2) = 0.00;
                    DECLARE @PorcentajeCumplimiento DECIMAL(5,2) = 0.00;

                    IF @Mes IS NULL OR @Mes = 0
                    BEGIN
                        -- ACUMULADO TODO EL AÑO
                        SET @SaldoAnteriorArrastrado = 0.00;

                        SELECT @IngresosMensuales = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND c.TipoCuota = 'RECURRENTE_MENSUAL' AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        SELECT @IngresosExtraordinarios = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND c.TipoCuota = 'EXTRAORDINARIA' AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        SELECT @IngresosDonaciones = ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId AND YEAR(FechaDonacion) = @AnioLectivo;

                        SELECT @TotalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId AND YEAR(FechaGasto) = @AnioLectivo;
                    END
                    ELSE
                    BEGIN
                        -- CORTE POR MES ESPECÍFICO

                        -- 1. Arrastre de Meses Anteriores
                        DECLARE @IngresosAnteriores DECIMAL(10,2) = 0.00;
                        DECLARE @EgresosAnteriores DECIMAL(10,2) = 0.00;

                        SELECT @IngresosAnteriores = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND MONTH(c.FechaVencimiento) < @Mes AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        SELECT @IngresosAnteriores = @IngresosAnteriores + ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId AND MONTH(FechaDonacion) < @Mes AND YEAR(FechaDonacion) = @AnioLectivo;

                        SELECT @EgresosAnteriores = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId AND MONTH(FechaGasto) < @Mes AND YEAR(FechaGasto) = @AnioLectivo;

                        SET @SaldoAnteriorArrastrado = (@IngresosAnteriores - @EgresosAnteriores);

                        -- 2. Ingresos del Mes Seleccionado
                        SELECT @IngresosMensuales = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND c.TipoCuota = 'RECURRENTE_MENSUAL' AND MONTH(c.FechaVencimiento) = @Mes AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        SELECT @IngresosExtraordinarios = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND c.TipoCuota = 'EXTRAORDINARIA' AND MONTH(c.FechaVencimiento) = @Mes AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        SELECT @IngresosDonaciones = ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId AND MONTH(FechaDonacion) = @Mes AND YEAR(FechaDonacion) = @AnioLectivo;

                        SELECT @TotalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId AND MONTH(FechaGasto) = @Mes AND YEAR(FechaGasto) = @AnioLectivo;
                    END

                    -- 3. Morosidad / Cumplimiento
                    SELECT @TotalPorCobrar = ISNULL(SUM(cd.MontoAsignado - cd.MontoPagado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId AND cd.EstadoPago <> 'COMPLETO' AND (@Mes IS NULL OR @Mes = 0 OR MONTH(c.FechaVencimiento) <= @Mes);

                    SELECT @TotalAsignado = ISNULL(SUM(cd.MontoAsignado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId AND (@Mes IS NULL OR @Mes = 0 OR MONTH(c.FechaVencimiento) <= @Mes);

                    IF @TotalAsignado > 0
                        SET @PorcentajeCumplimiento = (((@IngresosMensuales + @IngresosExtraordinarios + @IngresosDonaciones + CASE WHEN @Mes > 0 THEN @SaldoAnteriorArrastrado ELSE 0 END)) / @TotalAsignado) * 100;

                    -- Retorno (SEPARANDO IngresosDonaciones)
                    SELECT 
                        @SaldoAnteriorArrastrado AS SaldoAnteriorArrastrado,
                        @IngresosMensuales AS IngresosMensuales,
                        @IngresosExtraordinarios AS IngresosExtraordinarios,
                        @IngresosDonaciones AS IngresosDonaciones, -- 🚀 Campo independiente
                        (@IngresosMensuales + @IngresosExtraordinarios + @IngresosDonaciones) AS TotalIngresosMes,
                        @TotalEgresos AS TotalEgresosMes,
                        (@SaldoAnteriorArrastrado + @IngresosMensuales + @IngresosExtraordinarios + @IngresosDonaciones - @TotalEgresos) AS SaldoNetoEnCaja,
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
