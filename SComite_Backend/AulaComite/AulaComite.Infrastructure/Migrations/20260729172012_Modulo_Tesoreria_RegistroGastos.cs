using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modulo_Tesoreria_RegistroGastos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear Tabla GastosComite
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GastosComite')
                BEGIN
                    CREATE TABLE GastosComite (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        AulaId INT NOT NULL,
                        Concepto VARCHAR(150) NOT NULL,
                        Categoria VARCHAR(50) NOT NULL, -- MATERIALES, MANTENIMIENTO, ACTIVIDADES_EVENTOS, REFRIGERIOS, OTROS
                        Monto DECIMAL(10,2) NOT NULL,
                        FechaGasto DATE NOT NULL,
                        TipoComprobante VARCHAR(30) NOT NULL DEFAULT 'BOLETA', -- BOLETA, FACTURA, RECIBO_SIMPLE, SIN_COMPROBANTE
                        NumeroComprobante VARCHAR(50) NULL,
                        Proveedor NVARCHAR(150) NULL,
                        Observacion NVARCHAR(300) NULL,
                        UsuarioRegistro NVARCHAR(150) NOT NULL,
                        FechaRegistro DATETIME2 NOT NULL DEFAULT (DATEADD(HOUR, -5, GETUTCDATE())),
                        FOREIGN KEY (AulaId) REFERENCES Aulas(Id) ON DELETE CASCADE
                    );

                    CREATE INDEX IX_GastosComite_AulaId ON GastosComite(AulaId);
                    CREATE INDEX IX_GastosComite_Categoria ON GastosComite(Categoria);
                END
            ");

            // 2. SP: Registrar un nuevo Gasto
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Gastos_Registrar
                    @AulaId INT,
                    @Concepto VARCHAR(150),
                    @Categoria VARCHAR(50),
                    @Monto DECIMAL(10,2),
                    @FechaGasto DATE,
                    @TipoComprobante VARCHAR(30),
                    @NumeroComprobante VARCHAR(50) = NULL,
                    @Proveedor NVARCHAR(150) = NULL,
                    @Observacion NVARCHAR(300) = NULL,
                    @UsuarioRegistro NVARCHAR(150)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    INSERT INTO GastosComite (AulaId, Concepto, Categoria, Monto, FechaGasto, TipoComprobante, NumeroComprobante, Proveedor, Observacion, UsuarioRegistro)
                    VALUES (@AulaId, @Concepto, @Categoria, @Monto, @FechaGasto, @TipoComprobante, @NumeroComprobante, @Proveedor, @Observacion, @UsuarioRegistro);

                    SELECT SCOPE_IDENTITY();
                END
            ");

            // 3. SP: Obtener Gastos de un Aula y Resumen de Saldo de Caja
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Gastos_ObtenerPorAula
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Tabla de Gastos
                    SELECT 
                        g.Id,
                        g.AulaId,
                        g.Concepto,
                        g.Categoria,
                        g.Monto,
                        g.FechaGasto,
                        g.TipoComprobante,
                        g.NumeroComprobante,
                        g.Proveedor,
                        g.Observacion,
                        g.UsuarioRegistro,
                        g.FechaRegistro
                    FROM GastosComite g
                    WHERE g.AulaId = @AulaId
                    ORDER BY g.FechaGasto DESC, g.FechaRegistro DESC;
                END
            ");

            // 4. SP: Resumen Balance Financiero del Aula (Ingresos vs Egresos)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Gastos_ObtenerResumenCaja
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @TotalIngresos DECIMAL(10,2) = 0.00;
                    DECLARE @TotalEgresos DECIMAL(10,2) = 0.00;

                    -- Total Recaudado en Cuotas
                    SELECT @TotalIngresos = ISNULL(SUM(MontoPagado), 0.00)
                    FROM CuotaDetalleEstudiante cd
                    INNER JOIN Cuotas c ON cd.CuotaId = c.Id
                    WHERE c.AulaId = @AulaId;

                    -- Total Egresado en Gastos
                    SELECT @TotalEgresos = ISNULL(SUM(Monto), 0.00)
                    FROM GastosComite
                    WHERE AulaId = @AulaId;

                    SELECT 
                        @TotalIngresos AS TotalIngresos,
                        @TotalEgresos AS TotalEgresos,
                        (@TotalIngresos - @TotalEgresos) AS SaldoDisponible;
                END
            ");

            // 5. SP: Anular / Eliminar Gasto
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Gastos_Eliminar
                    @GastoId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DELETE FROM GastosComite WHERE Id = @GastoId;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Gastos_Eliminar");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Gastos_ObtenerResumenCaja");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Gastos_ObtenerPorAula");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Gastos_Registrar");
            migrationBuilder.Sql("DROP TABLE IF EXISTS GastosComite");
        }
    }
}
