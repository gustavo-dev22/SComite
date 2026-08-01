using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTablaDonacionesComite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear Tabla DonacionesComite
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DonacionesComite')
                BEGIN
                    CREATE TABLE DonacionesComite (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        AulaId INT NOT NULL,
                        Donante VARCHAR(150) NOT NULL, -- Ej: Prof. María López / Padrino / Anónimo / Padre
                        Monto DECIMAL(10,2) NOT NULL,
                        FechaDonacion DATE NOT NULL,
                        Concepto NVARCHAR(250) NOT NULL,
                        Observacion NVARCHAR(500) NULL,
                        FechaRegistro DATETIME2 NOT NULL DEFAULT (DATEADD(HOUR, -5, GETUTCDATE())),
                        FOREIGN KEY (AulaId) REFERENCES Aulas(Id) ON DELETE CASCADE
                    );

                    CREATE INDEX IX_DonacionesComite_AulaId ON DonacionesComite(AulaId);
                    CREATE INDEX IX_DonacionesComite_Fecha ON DonacionesComite(FechaDonacion);
                END
            ");

            // 2. SP: Listar Donaciones por Aula y Año
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Donaciones_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        d.Id,
                        d.AulaId,
                        d.Donante,
                        d.Monto,
                        d.FechaDonacion,
                        d.Concepto,
                        d.Observacion,
                        d.FechaRegistro
                    FROM DonacionesComite d
                    WHERE d.AulaId = @AulaId
                      AND YEAR(d.FechaDonacion) = @AnioLectivo
                    ORDER BY d.FechaDonacion DESC;
                END
            ");

            // 3. SP: Registrar Donación
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Donaciones_Guardar
                    @Id INT = 0,
                    @AulaId INT,
                    @Donante VARCHAR(150),
                    @Monto DECIMAL(10,2),
                    @FechaDonacion DATE,
                    @Concepto NVARCHAR(250),
                    @Observacion NVARCHAR(500) = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Id = 0
                    BEGIN
                        INSERT INTO DonacionesComite (AulaId, Donante, Monto, FechaDonacion, Concepto, Observacion)
                        VALUES (@AulaId, UPPER(@Donante), @Monto, @FechaDonacion, UPPER(@Concepto), @Observacion);

                        SELECT CAST(SCOPE_IDENTITY() AS INT);
                    END
                    ELSE
                    BEGIN
                        UPDATE DonacionesComite
                        SET Donante = UPPER(@Donante),
                            Monto = @Monto,
                            FechaDonacion = @FechaDonacion,
                            Concepto = UPPER(@Concepto),
                            Observacion = @Observacion
                        WHERE Id = @Id AND AulaId = @AulaId;

                        SELECT @Id;
                    END
                END
            ");

            // 4. SP: Eliminar Donación
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Donaciones_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DELETE FROM DonacionesComite WHERE Id = @Id AND AulaId = @AulaId;
                END
            ");

            // 5. Actualizar SP sp_Balance_ObtenerConsolidado para incluir Donaciones en los Ingresos
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

                        -- 🚀 Donaciones del año
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

                        -- 🚀 Donaciones del Mes
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

                    -- Retorno
                    SELECT 
                        @SaldoAnteriorArrastrado AS SaldoAnteriorArrastrado,
                        @IngresosMensuales AS IngresosMensuales,
                        (@IngresosExtraordinarios + @IngresosDonaciones) AS IngresosExtraordinarios, -- 🚀 Incluye Donaciones
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
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Donaciones_Eliminar;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Donaciones_Guardar;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Donaciones_ListarPorAula;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS DonacionesComite;");
        }
    }
}
