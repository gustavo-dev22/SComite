using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modulo_Tesoreria_BalanceCajaMensual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stored Procedure para calcular el Balance de Caja por Mes con Arrastre Histórico
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Gastos_ObtenerBalanceMensualCaja
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL -- NULL = Todo el Año, 3 = Marzo, 4 = Abril... 12 = Diciembre
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Mes IS NULL OR @Mes = 0
                    BEGIN
                        -- 🚀 CASO A: Balance Global de Todo el Año
                        DECLARE @GlobalIngresos DECIMAL(10,2) = 0.00;
                        DECLARE @GlobalEgresos DECIMAL(10,2) = 0.00;

                        SELECT @GlobalIngresos = ISNULL(SUM(MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId;

                        SELECT @GlobalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId;

                        SELECT 
                            0.00 AS SaldoAnteriorArrastrado,
                            @GlobalIngresos AS IngresosDelMes,
                            @GlobalEgresos AS EgresosDelMes,
                            (@GlobalIngresos - @GlobalEgresos) AS SaldoDisponibleReal;
                    END
                    ELSE
                    BEGIN
                        -- 🚀 CASO B: Balance Específico del Mes Seleccionado (Con Arrastre Acumulado)

                        -- 1. Saldo Anterior Arrastrado (Todos los Ingresos acumulados HASTA el mes anterior MINUS Gastos HASTA el mes anterior)
                        DECLARE @IngresosAnteriores DECIMAL(10,2) = 0.00;
                        DECLARE @EgresosAnteriores DECIMAL(10,2) = 0.00;

                        -- Ingresos anteriores (Pagos realizados antes del primer día del mes actual)
                        SELECT @IngresosAnteriores = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND cd.FechaUltimoPago IS NOT NULL
                          AND MONTH(cd.FechaUltimoPago) < @Mes
                          AND YEAR(cd.FechaUltimoPago) = @AnioLectivo;

                        -- Gastos anteriores
                        SELECT @EgresosAnteriores = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND MONTH(FechaGasto) < @Mes
                          AND YEAR(FechaGasto) = @AnioLectivo;

                        DECLARE @SaldoArrastrado DECIMAL(10,2) = (@IngresosAnteriores - @EgresosAnteriores);

                        -- 2. Ingresos Efectivos dentro del Mes Seleccionado
                        DECLARE @IngresosMes DECIMAL(10,2) = 0.00;
                        SELECT @IngresosMes = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND cd.FechaUltimoPago IS NOT NULL
                          AND MONTH(cd.FechaUltimoPago) = @Mes
                          AND YEAR(cd.FechaUltimoPago) = @AnioLectivo;

                        -- 3. Egresos/Gastos del Mes Seleccionado
                        DECLARE @EgresosMes DECIMAL(10,2) = 0.00;
                        SELECT @EgresosMes = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND MONTH(FechaGasto) = @Mes
                          AND YEAR(FechaGasto) = @AnioLectivo;

                        -- 4. Devuelve el Balance del Mes
                        SELECT 
                            @SaldoArrastrado AS SaldoAnteriorArrastrado,
                            @IngresosMes AS IngresosDelMes,
                            @EgresosMes AS EgresosDelMes,
                            (@SaldoArrastrado + @IngresosMes - @EgresosMes) AS SaldoDisponibleReal;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Gastos_ObtenerBalanceMensualCaja");
        }
    }
}
