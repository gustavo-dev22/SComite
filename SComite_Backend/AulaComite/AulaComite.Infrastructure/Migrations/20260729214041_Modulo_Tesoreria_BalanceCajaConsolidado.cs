using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modulo_Tesoreria_BalanceCajaConsolidado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. SP: Obtener el Balance Consolidado Ejecutivo del Aula
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Balance_ObtenerConsolidado
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- A. Total Recaudado en Cuotas Mensuales
                    DECLARE @IngresosMensuales DECIMAL(10,2) = 0.00;
                    SELECT @IngresosMensuales = ISNULL(SUM(cd.MontoPagado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId AND c.TipoCuota = 'RECURRENTE_MENSUAL';

                    -- B. Total Recaudado en Cuotas Extraordinarias / Actividades
                    DECLARE @IngresosExtraordinarios DECIMAL(10,2) = 0.00;
                    SELECT @IngresosExtraordinarios = ISNULL(SUM(cd.MontoPagado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId AND c.TipoCuota = 'EXTRAORDINARIA';

                    -- C. Total Deuda Pendiente por Cobrar
                    DECLARE @TotalPorCobrar DECIMAL(10,2) = 0.00;
                    SELECT @TotalPorCobrar = ISNULL(SUM(cd.MontoAsignado - cd.MontoPagado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId AND cd.EstadoPago <> 'COMPLETO';

                    -- D. Total Egresos / Gastos
                    DECLARE @TotalEgresos DECIMAL(10,2) = 0.00;
                    SELECT @TotalEgresos = ISNULL(SUM(Monto), 0.00)
                    FROM GastosComite
                    WHERE AulaId = @AulaId;

                    -- E. Porcentaje de Cumplimiento
                    DECLARE @TotalAsignado DECIMAL(10,2) = 0.00;
                    DECLARE @PorcentajeCumplimiento DECIMAL(5,2) = 0.00;
                    
                    SELECT @TotalAsignado = ISNULL(SUM(cd.MontoAsignado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId;

                    IF @TotalAsignado > 0
                        SET @PorcentajeCumplimiento = ((@IngresosMensuales + @IngresosExtraordinarios) / @TotalAsignado) * 100;

                    SELECT 
                        @IngresosMensuales AS IngresosMensuales,
                        @IngresosExtraordinarios AS IngresosExtraordinarios,
                        (@IngresosMensuales + @IngresosExtraordinarios) AS TotalIngresos,
                        @TotalEgresos AS TotalEgresos,
                        ((@IngresosMensuales + @IngresosExtraordinarios) - @TotalEgresos) AS SaldoNetoEnCaja,
                        @TotalPorCobrar AS TotalPorCobrar,
                        @PorcentajeCumplimiento AS PorcentajeCumplimiento;
                END
            ");

            // 2. SP: Obtener Desglose de Gastos por Categoría
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Balance_ObtenerGastosPorCategoria
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        Categoria,
                        ISNULL(SUM(Monto), 0.00) AS TotalMonto,
                        COUNT(Id) AS CantidadRegistros
                    FROM GastosComite
                    WHERE AulaId = @AulaId
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
