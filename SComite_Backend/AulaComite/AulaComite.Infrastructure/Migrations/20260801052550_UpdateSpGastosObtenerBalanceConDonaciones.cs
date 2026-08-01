using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpGastosObtenerBalanceConDonaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Gastos_ObtenerBalanceMensualCaja]
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL -- NULL o 0 = Todo el Año
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Mes IS NULL OR @Mes = 0
                    BEGIN
                        -- 🚀 CASO A: Balance Global de Todo el Año (Acumulado)
                        DECLARE @GlobalCuotas DECIMAL(10,2) = 0.00;
                        DECLARE @GlobalDonaciones DECIMAL(10,2) = 0.00;
                        DECLARE @GlobalEgresos DECIMAL(10,2) = 0.00;

                        -- Suma de cuotas (ordinarias y extraordinarias)
                        SELECT @GlobalCuotas = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        -- Suma de donaciones voluntarias del año
                        SELECT @GlobalDonaciones = ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId AND YEAR(FechaDonacion) = @AnioLectivo;

                        -- Suma de egresos/gastos del año
                        SELECT @GlobalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId AND YEAR(FechaGasto) = @AnioLectivo;

                        DECLARE @GlobalIngresosTotales DECIMAL(10,2) = (@GlobalCuotas + @GlobalDonaciones);

                        SELECT 
                            0.00 AS SaldoAnteriorArrastrado,
                            @GlobalIngresosTotales AS IngresosDelMes,
                            @GlobalDonaciones AS MontoDonacionesMes, -- 🚀 Campo explícito de Donaciones
                            @GlobalEgresos AS EgresosDelMes,
                            (@GlobalIngresosTotales - @GlobalEgresos) AS SaldoDisponibleReal;
                    END
                    ELSE
                    BEGIN
                        -- 🚀 CASO B: Balance Específico por Mes (Con Arrastre de Saldo)

                        -- 1. Saldo Anterior Arrastrado (Cuotas + Donaciones anteriores MINUS Gastos anteriores)
                        DECLARE @CuotasAnteriores DECIMAL(10,2) = 0.00;
                        DECLARE @DonacionesAnteriores DECIMAL(10,2) = 0.00;
                        DECLARE @EgresosAnteriores DECIMAL(10,2) = 0.00;

                        SELECT @CuotasAnteriores = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND MONTH(c.FechaVencimiento) < @Mes 
                          AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        SELECT @DonacionesAnteriores = ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId 
                          AND MONTH(FechaDonacion) < @Mes 
                          AND YEAR(FechaDonacion) = @AnioLectivo;

                        SELECT @EgresosAnteriores = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND MONTH(FechaGasto) < @Mes 
                          AND YEAR(FechaGasto) = @AnioLectivo;

                        DECLARE @SaldoArrastrado DECIMAL(10,2) = (@CuotasAnteriores + @DonacionesAnteriores - @EgresosAnteriores);

                        -- 2. Ingresos del Mes (Cuotas de Cobro del Mes + Donaciones Recibidas en el Mes)
                        DECLARE @CuotasMes DECIMAL(10,2) = 0.00;
                        DECLARE @DonacionesMes DECIMAL(10,2) = 0.00;

                        SELECT @CuotasMes = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND MONTH(c.FechaVencimiento) = @Mes 
                          AND YEAR(c.FechaVencimiento) = @AnioLectivo;

                        SELECT @DonacionesMes = ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId 
                          AND MONTH(FechaDonacion) = @Mes 
                          AND YEAR(FechaDonacion) = @AnioLectivo;

                        DECLARE @IngresosTotalesMes DECIMAL(10,2) = (@CuotasMes + @DonacionesMes);

                        -- 3. Egresos del Mes
                        DECLARE @EgresosMes DECIMAL(10,2) = 0.00;

                        SELECT @EgresosMes = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND MONTH(FechaGasto) = @Mes 
                          AND YEAR(FechaGasto) = @AnioLectivo;

                        -- 4. Devolver Balance del Mes
                        SELECT 
                            @SaldoArrastrado AS SaldoAnteriorArrastrado,
                            @IngresosTotalesMes AS IngresosDelMes,
                            @DonacionesMes AS MontoDonacionesMes, -- 🚀 Campo explícito de Donaciones
                            @EgresosMes AS EgresosDelMes,
                            (@SaldoArrastrado + @IngresosTotalesMes - @EgresosMes) AS SaldoDisponibleReal;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
