using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modulo_Auditoria_Logs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear la Tabla LogsSistema si no existe previamente
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LogsSistema')
                BEGIN
                    CREATE TABLE LogsSistema (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Fecha DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        Nivel VARCHAR(20) NOT NULL, -- INFO, WARNING, ERROR, CRITICAL
                        Modulo VARCHAR(50) NOT NULL, -- AUTH, AULAS, ESTUDIANTES, COMITE, etc.
                        Accion VARCHAR(100) NOT NULL,
                        Usuario VARCHAR(100) NULL,
                        IP VARCHAR(45) NULL,
                        Mensaje NVARCHAR(MAX) NOT NULL,
                        DetalleException NVARCHAR(MAX) NULL
                    );

                    CREATE INDEX IX_LogsSistema_Fecha ON LogsSistema(Fecha DESC);
                    CREATE INDEX IX_LogsSistema_Nivel ON LogsSistema(Nivel);
                    CREATE INDEX IX_LogsSistema_Modulo ON LogsSistema(Modulo);
                END
            ");

            // 2. STORED PROCEDURE: Registrar Log
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Logs_Registrar
                    @Nivel VARCHAR(20),
                    @Modulo VARCHAR(50),
                    @Accion VARCHAR(100),
                    @Usuario VARCHAR(100) = NULL,
                    @IP VARCHAR(45) = NULL,
                    @Mensaje NVARCHAR(MAX),
                    @DetalleException NVARCHAR(MAX) = NULL
                AS
                BEGIN
                    SET NOCOUNT OFF;

                    INSERT INTO LogsSistema (Fecha, Nivel, Modulo, Accion, Usuario, IP, Mensaje, DetalleException)
                    VALUES (GETUTCDATE(), @Nivel, @Modulo, @Accion, @Usuario, @IP, @Mensaje, @DetalleException);

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ");

            // 3. STORED PROCEDURE: Consultar Logs Paginados con Filtros Dinámicos
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Logs_ObtenerFiltrados
                    @FechaInicio DATETIME2 = NULL,
                    @FechaFin DATETIME2 = NULL,
                    @Nivel VARCHAR(20) = NULL,
                    @Modulo VARCHAR(50) = NULL,
                    @Busqueda NVARCHAR(100) = NULL,
                    @Pagina INT = 1,
                    @TamanoPagina INT = 20
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Ajustar rango de fechas por defecto
                    IF @FechaInicio IS NULL SET @FechaInicio = DATEADD(DAY, -30, GETUTCDATE());
                    IF @FechaFin IS NULL SET @FechaFin = GETUTCDATE();

                    -- Calcular salto para paginación
                    DECLARE @Skip INT = (@Pagina - 1) * @TamanoPagina;

                    -- Consulta principal
                    SELECT 
                        Id, Fecha, Nivel, Modulo, Accion, Usuario, IP, Mensaje, DetalleException,
                        COUNT(*) OVER() AS TotalRegistros
                    FROM LogsSistema
                    WHERE Fecha BETWEEN @FechaInicio AND @FechaFin
                      AND (@Nivel IS NULL OR @Nivel = '' OR Nivel = @Nivel)
                      AND (@Modulo IS NULL OR @Modulo = '' OR Modulo = @Modulo)
                      AND (@Busqueda IS NULL OR @Busqueda = '' 
                           OR Mensaje LIKE '%' + @Busqueda + '%' 
                           OR Accion LIKE '%' + @Busqueda + '%' 
                           OR Usuario LIKE '%' + @Busqueda + '%')
                    ORDER BY Fecha DESC
                    OFFSET @Skip ROWS
                    FETCH NEXT @TamanoPagina ROWS ONLY;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Logs_Registrar");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Logs_ObtenerFiltrados");
            migrationBuilder.Sql("DROP TABLE IF EXISTS LogsSistema");
        }
    }
}
