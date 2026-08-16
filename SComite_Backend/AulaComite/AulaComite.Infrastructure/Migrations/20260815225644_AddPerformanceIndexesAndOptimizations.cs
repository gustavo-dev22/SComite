using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexesAndOptimizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🚀 T3.2: Índices de rendimiento sobre claves foráneas y columnas de filtrado.
            // Se crean de forma IDEMPOTENTE (IF NOT EXISTS) para ser seguros en re-ejecución.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Estudiantes_AulaId' AND object_id = OBJECT_ID('Estudiantes'))
                    CREATE NONCLUSTERED INDEX IX_Estudiantes_AulaId ON Estudiantes(AulaId);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CuotaDetalleEstudiante_Cuota_Estudiante' AND object_id = OBJECT_ID('CuotaDetalleEstudiante'))
                    CREATE NONCLUSTERED INDEX IX_CuotaDetalleEstudiante_Cuota_Estudiante ON CuotaDetalleEstudiante(CuotaId, EstudianteId);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Gastos_AulaId_Fecha' AND object_id = OBJECT_ID('GastosComite'))
                    CREATE NONCLUSTERED INDEX IX_Gastos_AulaId_Fecha ON GastosComite(AulaId, FechaGasto);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Anuncios_AulaId' AND object_id = OBJECT_ID('AnunciosComite'))
                    CREATE NONCLUSTERED INDEX IX_Anuncios_AulaId ON AnunciosComite(AulaId);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActasAsamblea_AulaId' AND object_id = OBJECT_ID('ActasAsambleaComite'))
                    CREATE NONCLUSTERED INDEX IX_ActasAsamblea_AulaId ON ActasAsambleaComite(AulaId);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ComiteIntegrantes_AulaId' AND object_id = OBJECT_ID('ComiteIntegrantes'))
                    CREATE NONCLUSTERED INDEX IX_ComiteIntegrantes_AulaId ON ComiteIntegrantes(AulaId);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AnuncioLecturas_AnuncioId' AND object_id = OBJECT_ID('AnuncioLecturasEstudiante'))
                    CREATE NONCLUSTERED INDEX IX_AnuncioLecturas_AnuncioId ON AnuncioLecturasEstudiante(AnuncioId);
            ");

            // 🚀 T3.2: Reescritura SARGable de sp_Balance_ObtenerConsolidado.
            // Se reemplazan filtros non-SARGable MONTH()/YEAR() por rangos continuos de
            // fecha (>= @Inicio AND < @Fin) para aprovechar IX_Gastos_AulaId_Fecha y los
            // índices de FechaVencimiento. El comportamiento contable es idéntico.
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Balance_ObtenerConsolidado]
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL -- NULL o 0 = Acumulado Todo el Año
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Rangos continuos de fecha (SARGable)
                    DECLARE @InicioAnio DATE = DATEFROMPARTS(@AnioLectivo, 1, 1);
                    DECLARE @FinAnio DATE = DATEADD(YEAR, 1, @InicioAnio);
                    DECLARE @InicioMes DATE = DATEADD(MONTH, ISNULL(NULLIF(@Mes, 0), 1) - 1, @InicioAnio);
                    DECLARE @FinMes DATE = DATEADD(MONTH, 1, @InicioMes);

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
                        WHERE c.AulaId = @AulaId AND c.TipoCuota = 'RECURRENTE_MENSUAL'
                          AND c.FechaVencimiento >= @InicioAnio AND c.FechaVencimiento < @FinAnio;

                        SELECT @IngresosExtraordinarios = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND c.TipoCuota = 'EXTRAORDINARIA'
                          AND c.FechaVencimiento >= @InicioAnio AND c.FechaVencimiento < @FinAnio;

                        SELECT @IngresosDonaciones = ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId
                          AND FechaDonacion >= @InicioAnio AND FechaDonacion < @FinAnio;

                        SELECT @TotalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId
                          AND FechaGasto >= @InicioAnio AND FechaGasto < @FinAnio;
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
                        WHERE c.AulaId = @AulaId
                          AND c.FechaVencimiento >= @InicioAnio AND c.FechaVencimiento < @InicioMes;

                        SELECT @IngresosAnteriores = @IngresosAnteriores + ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId
                          AND FechaDonacion >= @InicioAnio AND FechaDonacion < @InicioMes;

                        SELECT @EgresosAnteriores = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId
                          AND FechaGasto >= @InicioAnio AND FechaGasto < @InicioMes;

                        SET @SaldoAnteriorArrastrado = (@IngresosAnteriores - @EgresosAnteriores);

                        -- 2. Ingresos del Mes Seleccionado
                        SELECT @IngresosMensuales = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND c.TipoCuota = 'RECURRENTE_MENSUAL'
                          AND c.FechaVencimiento >= @InicioMes AND c.FechaVencimiento < @FinMes;

                        SELECT @IngresosExtraordinarios = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId AND c.TipoCuota = 'EXTRAORDINARIA'
                          AND c.FechaVencimiento >= @InicioMes AND c.FechaVencimiento < @FinMes;

                        SELECT @IngresosDonaciones = ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId
                          AND FechaDonacion >= @InicioMes AND FechaDonacion < @FinMes;

                        SELECT @TotalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId
                          AND FechaGasto >= @InicioMes AND FechaGasto < @FinMes;
                    END

                    -- 3. Morosidad / Cumplimiento
                    SELECT @TotalPorCobrar = ISNULL(SUM(cd.MontoAsignado - cd.MontoPagado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId AND cd.EstadoPago <> 'COMPLETO'
                      AND (@Mes IS NULL OR @Mes = 0 OR c.FechaVencimiento < @FinMes);

                    SELECT @TotalAsignado = ISNULL(SUM(cd.MontoAsignado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId
                      AND (@Mes IS NULL OR @Mes = 0 OR c.FechaVencimiento < @FinMes);

                    IF @TotalAsignado > 0
                        SET @PorcentajeCumplimiento = (((@IngresosMensuales + @IngresosExtraordinarios + @IngresosDonaciones + CASE WHEN @Mes > 0 THEN @SaldoAnteriorArrastrado ELSE 0 END)) / @TotalAsignado) * 100;

                    -- Retorno (SEPARANDO IngresosDonaciones)
                    SELECT 
                        @SaldoAnteriorArrastrado AS SaldoAnteriorArrastrado,
                        @IngresosMensuales AS IngresosMensuales,
                        @IngresosExtraordinarios AS IngresosExtraordinarios,
                        @IngresosDonaciones AS IngresosDonaciones,
                        (@IngresosMensuales + @IngresosExtraordinarios + @IngresosDonaciones) AS TotalIngresosMes,
                        @TotalEgresos AS TotalEgresosMes,
                        (@SaldoAnteriorArrastrado + @IngresosMensuales + @IngresosExtraordinarios + @IngresosDonaciones - @TotalEgresos) AS SaldoNetoEnCaja,
                        @TotalPorCobrar AS TotalPorCobrar,
                        @PorcentajeCumplimiento AS PorcentajeCumplimiento;
                END
            ");

            // 🚀 T3.2: Reescritura SARGable de sp_Balance_ObtenerGastosPorCategoria.
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Balance_ObtenerGastosPorCategoria
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Rango continuo de fecha (SARGable) para el mes seleccionado
                    DECLARE @InicioMes DATE = DATEFROMPARTS(@AnioLectivo, ISNULL(NULLIF(@Mes, 0), 1), 1);
                    DECLARE @FinMes DATE = DATEADD(MONTH, 1, @InicioMes);

                    SELECT 
                        Categoria,
                        ISNULL(SUM(Monto), 0.00) AS TotalMonto,
                        COUNT(Id) AS CantidadRegistros
                    FROM GastosComite
                    WHERE AulaId = @AulaId
                      AND (@Mes IS NULL OR @Mes = 0 OR (FechaGasto >= @InicioMes AND FechaGasto < @FinMes))
                    GROUP BY Categoria
                    ORDER BY TotalMonto DESC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Los índices se crean de forma idempotente; sp_Balance_* son equivalentes en
            // comportamiento, por lo que la reversión no requiere operaciones destructivas.
        }
    }
}