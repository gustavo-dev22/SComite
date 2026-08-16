using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizacionSargableYDeudaTecnica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🛡️ P1: Corregir zona horaria en sp_Apoderado_ObtenerCuotasPendientes.
            // El cálculo de estado VENCIDO usaba CAST(GETDATE() AS DATE) (hora del servidor);
            // ahora usa hora Perú (UTC-5), consistente con el resto del proyecto.
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Apoderado_ObtenerCuotasPendientes]
                    @EstudianteId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AulaId INT;

                    SELECT TOP 1 @AulaId = a.Id
                    FROM Estudiantes e
                    JOIN Aulas a ON a.Id = e.AulaId
                    JOIN PeriodosLectivos p ON p.Id = a.PeriodoId
                    WHERE e.Id = @EstudianteId 
                      AND p.Anio = @AnioLectivo
                      AND e.Estado = 1;

                    IF @AulaId IS NULL RETURN;

                    SELECT 
                        c.Id AS CuotaId,
                        d.Id AS CuotaDetalleId,
                        c.Concepto,
                        c.TipoCuota,
                        c.MontoIndividual AS MontoTotalCuota,
                        c.FechaVencimiento,
                        ISNULL(d.MontoPagado, 0) AS MontoPagado,
                        CASE 
                            WHEN UPPER(ISNULL(d.EstadoPago, 'PENDIENTE')) = 'EXONERADO' THEN 0.00
                            ELSE (c.MontoIndividual - ISNULL(d.MontoPagado, 0))
                        END AS MontoPendiente,
                        ISNULL(d.EstadoPago, 'PENDIENTE') AS EstadoPago,
                        CASE 
                            WHEN UPPER(ISNULL(d.EstadoPago, 'PENDIENTE')) = 'EXONERADO' THEN 'EXONERADO'
                            WHEN UPPER(ISNULL(d.EstadoPago, 'PENDIENTE')) IN ('COMPLETO', 'PAGADO', 'VALIDADO') THEN 'PAGADO'
                            WHEN c.FechaVencimiento < CAST(DATEADD(HOUR, -5, GETUTCDATE()) AS DATE) THEN 'VENCIDO'
                            ELSE 'PENDIENTE'
                        END AS EstadoVisual,
                        d.MotivoExoneracion,
                        d.FechaUltimoPago
                    FROM Cuotas c
                    LEFT JOIN CuotaDetalleEstudiante d ON d.CuotaId = c.Id AND d.EstudianteId = @EstudianteId
                    WHERE c.AulaId = @AulaId
                      AND (c.Estado IS NULL OR CAST(c.Estado AS VARCHAR(50)) NOT IN ('INACTIVO', 'ELIMINADO', '0'))
                    ORDER BY c.FechaVencimiento ASC;
                END
            ");

            // 🛡️ P2: Eliminar el índice duplicado de Anuncios. La tabla original creó
            // IX_AnunciosComite_AulaId; la migración de performance creó IX_Anuncios_AulaId
            // sobre la misma columna. Se conserva el original y se elimina el duplicado.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Anuncios_AulaId' AND object_id = OBJECT_ID('AnunciosComite'))
                    DROP INDEX IX_Anuncios_AulaId ON AnunciosComite;
            ");

            // 🛡️ P2: Índices compuestos (AulaId + Fecha) para las consultas de balance y
            // listados por aula filtrados por fecha (SARGable). Idempotentes.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Cuotas_AulaId_FechaVencimiento' AND object_id = OBJECT_ID('Cuotas'))
                    CREATE NONCLUSTERED INDEX IX_Cuotas_AulaId_FechaVencimiento ON Cuotas(AulaId, FechaVencimiento);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Donaciones_AulaId_FechaDonacion' AND object_id = OBJECT_ID('DonacionesComite'))
                    CREATE NONCLUSTERED INDEX IX_Donaciones_AulaId_FechaDonacion ON DonacionesComite(AulaId, FechaDonacion);
            ");

            // 🛡️ P2: sp_Donaciones_ListarPorAula SARGable (rangos de fecha en lugar de MONTH()/YEAR()).
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Donaciones_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL -- NULL o 0 = Todo el año
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @FechaInicioAnio DATE = DATEFROMPARTS(@AnioLectivo, 1, 1);
                    DECLARE @FechaFinAnio DATE = DATEADD(YEAR, 1, @FechaInicioAnio);
                    DECLARE @FechaInicioMes DATE = DATEADD(MONTH, ISNULL(NULLIF(@Mes, 0), 1) - 1, @FechaInicioAnio);
                    DECLARE @FechaFinMes DATE = DATEADD(MONTH, 1, @FechaInicioMes);

                    SELECT TOP 200 
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
                      AND d.FechaDonacion >= @FechaInicioAnio AND d.FechaDonacion < @FechaFinAnio
                      AND (@Mes IS NULL OR @Mes = 0 OR (d.FechaDonacion >= @FechaInicioMes AND d.FechaDonacion < @FechaFinMes))
                    ORDER BY d.FechaDonacion DESC;
                END
            ");

            // 🛡️ P2: sp_ActasAsamblea_ListarPorAula SARGable.
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_ActasAsamblea_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @FechaInicio DATE = DATEFROMPARTS(@AnioLectivo, 1, 1);
                    DECLARE @FechaFin DATE = DATEADD(YEAR, 1, @FechaInicio);

                    SELECT TOP 200 
                        a.Id,
                        a.AulaId,
                        a.NumeroActa,
                        a.Titulo,
                        a.FechaReunion,
                        a.AgendaAcuerdos,
                        a.EstadoActa,
                        a.UrlDocumentoPdf,
                        a.UsuarioRegistro,
                        a.FechaRegistro,
                        a.UsuarioActualizacion,
                        a.FechaActualizacion,
                        a.Estado
                    FROM ActasAsambleaComite a
                    WHERE a.AulaId = @AulaId
                      AND a.FechaReunion >= @FechaInicio AND a.FechaReunion < @FechaFin
                      AND a.Estado = 1
                    ORDER BY a.FechaReunion DESC, a.Id DESC;
                END
            ");

            // 🛡️ P2: sp_Anuncios_ListarPorAula SARGable.
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Anuncios_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @FechaInicio DATE = DATEFROMPARTS(@AnioLectivo, 1, 1);
                    DECLARE @FechaFin DATE = DATEADD(YEAR, 1, @FechaInicio);

                    SELECT TOP 200 
                        a.Id,
                        a.AulaId,
                        a.Titulo,
                        a.Contenido,
                        a.Categoria,
                        a.EsFijado,
                        a.UrlAdjunto,
                        a.UsuarioRegistro,
                        a.FechaPublicacion,
                        a.CantidadVistas,
                        a.Estado
                    FROM AnunciosComite a
                    WHERE a.AulaId = @AulaId
                      AND a.FechaPublicacion >= @FechaInicio AND a.FechaPublicacion < @FechaFin
                      AND a.Estado = 1
                    ORDER BY a.EsFijado DESC, a.FechaPublicacion DESC;
                END
            ");

            // 🛡️ P2: sp_Gastos_ObtenerBalanceMensualCaja SARGable (rangos en lugar de MONTH()/YEAR()).
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Gastos_ObtenerBalanceMensualCaja]
                    @AulaId INT,
                    @AnioLectivo INT,
                    @Mes INT = NULL -- NULL o 0 = Todo el Año
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @FechaInicioAnio DATE = DATEFROMPARTS(@AnioLectivo, 1, 1);
                    DECLARE @FechaFinAnio DATE = DATEADD(YEAR, 1, @FechaInicioAnio);
                    DECLARE @FechaInicioMes DATE = DATEADD(MONTH, ISNULL(NULLIF(@Mes, 0), 1) - 1, @FechaInicioAnio);
                    DECLARE @FechaFinMes DATE = DATEADD(MONTH, 1, @FechaInicioMes);

                    IF @Mes IS NULL OR @Mes = 0
                    BEGIN
                        -- 🚀 CASO A: Balance Global de Todo el Año (Acumulado)
                        DECLARE @GlobalCuotas DECIMAL(10,2) = 0.00;
                        DECLARE @GlobalDonaciones DECIMAL(10,2) = 0.00;
                        DECLARE @GlobalEgresos DECIMAL(10,2) = 0.00;

                        SELECT @GlobalCuotas = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND c.FechaVencimiento >= @FechaInicioAnio AND c.FechaVencimiento < @FechaFinAnio;

                        SELECT @GlobalDonaciones = ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId 
                          AND FechaDonacion >= @FechaInicioAnio AND FechaDonacion < @FechaFinAnio;

                        SELECT @GlobalEgresos = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND FechaGasto >= @FechaInicioAnio AND FechaGasto < @FechaFinAnio;

                        DECLARE @GlobalIngresosTotales DECIMAL(10,2) = (@GlobalCuotas + @GlobalDonaciones);

                        SELECT 
                            0.00 AS SaldoAnteriorArrastrado,
                            @GlobalIngresosTotales AS IngresosDelMes,
                            @GlobalDonaciones AS MontoDonacionesMes,
                            @GlobalEgresos AS EgresosDelMes,
                            (@GlobalIngresosTotales - @GlobalEgresos) AS SaldoDisponibleReal;
                    END
                    ELSE
                    BEGIN
                        -- 🚀 CASO B: Balance Específico por Mes (Con Arrastre de Saldo)

                        DECLARE @CuotasAnteriores DECIMAL(10,2) = 0.00;
                        DECLARE @DonacionesAnteriores DECIMAL(10,2) = 0.00;
                        DECLARE @EgresosAnteriores DECIMAL(10,2) = 0.00;

                        SELECT @CuotasAnteriores = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND c.FechaVencimiento >= @FechaInicioAnio AND c.FechaVencimiento < @FechaInicioMes;

                        SELECT @DonacionesAnteriores = ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId 
                          AND FechaDonacion >= @FechaInicioAnio AND FechaDonacion < @FechaInicioMes;

                        SELECT @EgresosAnteriores = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND FechaGasto >= @FechaInicioAnio AND FechaGasto < @FechaInicioMes;

                        DECLARE @SaldoArrastrado DECIMAL(10,2) = (@CuotasAnteriores + @DonacionesAnteriores - @EgresosAnteriores);

                        DECLARE @CuotasMes DECIMAL(10,2) = 0.00;
                        DECLARE @DonacionesMes DECIMAL(10,2) = 0.00;

                        SELECT @CuotasMes = ISNULL(SUM(cd.MontoPagado), 0.00)
                        FROM CuotaDetalleEstudiante cd
                        INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND c.FechaVencimiento >= @FechaInicioMes AND c.FechaVencimiento < @FechaFinMes;

                        SELECT @DonacionesMes = ISNULL(SUM(Monto), 0.00)
                        FROM DonacionesComite
                        WHERE AulaId = @AulaId 
                          AND FechaDonacion >= @FechaInicioMes AND FechaDonacion < @FechaFinMes;

                        DECLARE @IngresosTotalesMes DECIMAL(10,2) = (@CuotasMes + @DonacionesMes);

                        DECLARE @EgresosMes DECIMAL(10,2) = 0.00;

                        SELECT @EgresosMes = ISNULL(SUM(Monto), 0.00)
                        FROM GastosComite
                        WHERE AulaId = @AulaId 
                          AND FechaGasto >= @FechaInicioMes AND FechaGasto < @FechaFinMes;

                        SELECT 
                            @SaldoArrastrado AS SaldoAnteriorArrastrado,
                            @IngresosTotalesMes AS IngresosDelMes,
                            @DonacionesMes AS MontoDonacionesMes,
                            @EgresosMes AS EgresosDelMes,
                            (@SaldoArrastrado + @IngresosTotalesMes - @EgresosMes) AS SaldoDisponibleReal;
                    END
                END
            ");

            // 🛡️ P2: sp_Apoderado_ObtenerTransparenciaBalanceAula SARGable.
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Apoderado_ObtenerTransparenciaBalanceAula
                    @AulaId INT,
                    @Anio INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    SET LANGUAGE Spanish;

                    DECLARE @FechaInicio DATE = DATEFROMPARTS(@Anio, 1, 1);
                    DECLARE @FechaFin DATE = DATEADD(YEAR, 1, @FechaInicio);

                    -- 1. Resumen General Acumulado del Año (Cuotas Pagadas + Donaciones)
                    SELECT 
                        (
                            ISNULL((
                                SELECT SUM(d.MontoPagado) 
                                FROM CuotaDetalleEstudiante d
                                INNER JOIN Cuotas c ON d.CuotaId = c.Id
                                WHERE c.AulaId = @AulaId 
                                  AND UPPER(TRIM(d.EstadoPago)) IN ('PAGADO', 'VALIDADO', 'COMPLETO', 'APROBADO', 'PARCIAL')
                                  AND d.MontoPagado > 0
                                  AND c.FechaVencimiento >= @FechaInicio AND c.FechaVencimiento < @FechaFin
                            ), 0)
                            +
                            ISNULL((
                                SELECT SUM(don.Monto)
                                FROM DonacionesComite don
                                WHERE don.AulaId = @AulaId
                                  AND don.Monto > 0
                                  AND don.FechaDonacion >= @FechaInicio AND don.FechaDonacion < @FechaFin
                            ), 0)
                        ) AS TotalIngresos,
                        ISNULL((
                            SELECT SUM(g.Monto) 
                            FROM GastosComite g 
                            WHERE g.AulaId = @AulaId
                              AND g.FechaGasto >= @FechaInicio AND g.FechaGasto < @FechaFin
                        ), 0) AS TotalEgresos;

                    -- 2. Balance Detallado por Mes (Cuotas + Donaciones vs Egresos)
                    WITH MovimientosMensuales AS (
                        -- A) Ingresos por Cuotas
                        SELECT 
                            MONTH(c.FechaVencimiento) AS MesNum,
                            SUM(d.MontoPagado) AS Ingresos,
                            0.00 AS Egresos
                        FROM CuotaDetalleEstudiante d
                        INNER JOIN Cuotas c ON d.CuotaId = c.Id
                        WHERE c.AulaId = @AulaId 
                          AND UPPER(TRIM(d.EstadoPago)) IN ('PAGADO', 'VALIDADO', 'COMPLETO', 'APROBADO', 'PARCIAL')
                          AND d.MontoPagado > 0
                          AND c.FechaVencimiento >= @FechaInicio AND c.FechaVencimiento < @FechaFin
                        GROUP BY MONTH(c.FechaVencimiento)

                        UNION ALL

                        -- B) Ingresos por Donaciones
                        SELECT 
                            MONTH(don.FechaDonacion) AS MesNum,
                            SUM(don.Monto) AS Ingresos,
                            0.00 AS Egresos
                        FROM DonacionesComite don
                        WHERE don.AulaId = @AulaId
                          AND don.Monto > 0
                          AND don.FechaDonacion >= @FechaInicio AND don.FechaDonacion < @FechaFin
                        GROUP BY MONTH(don.FechaDonacion)

                        UNION ALL

                        -- C) Egresos por Gastos
                        SELECT 
                            MONTH(g.FechaGasto) AS MesNum,
                            0.00 AS Ingresos,
                            SUM(g.Monto) AS Egresos
                        FROM GastosComite g
                        WHERE g.AulaId = @AulaId
                          AND g.FechaGasto >= @FechaInicio AND g.FechaGasto < @FechaFin
                        GROUP BY MONTH(g.FechaGasto)
                    ),
                    Mensual AS (
                        SELECT 
                            MesNum,
                            SUM(Ingresos) AS IngresosMes,
                            SUM(Egresos) AS EgresosMes
                        FROM MovimientosMensuales
                        GROUP BY MesNum
                    ),
                    Acumulado AS (
                        SELECT 
                            MesNum,
                            IngresosMes,
                            EgresosMes,
                            SUM(IngresosMes) OVER (ORDER BY MesNum) AS IngresosAcumulados,
                            SUM(EgresosMes) OVER (ORDER BY MesNum) AS EgresosAcumulados
                        FROM Mensual
                    )
                    SELECT 
                        @Anio AS Anio,
                        MesNum,
                        UPPER(LEFT(DATENAME(MONTH, DATEFROMPARTS(@Anio, MesNum, 1)), 1)) + 
                        LOWER(SUBSTRING(DATENAME(MONTH, DATEFROMPARTS(@Anio, MesNum, 1)), 2, 20)) AS NombreMes,
                        (IngresosAcumulados - EgresosAcumulados + EgresosMes) AS TotalIngresosMes,
                        EgresosMes AS TotalEgresosMes,
                        (IngresosAcumulados - EgresosAcumulados) AS SaldoMes
                    FROM Acumulado
                    ORDER BY MesNum DESC;

                    -- 3. Listado Completo de Egresos
                    SELECT TOP 200 
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
                      AND g.FechaGasto >= @FechaInicio AND g.FechaGasto < @FechaFin
                    ORDER BY g.FechaGasto DESC;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La reversión de índices/SPs no es destructiva de datos; los SPs anteriores
            // son funcionalmente equivalentes. Se deja vacío (patrón del proyecto).
        }
    }
}
