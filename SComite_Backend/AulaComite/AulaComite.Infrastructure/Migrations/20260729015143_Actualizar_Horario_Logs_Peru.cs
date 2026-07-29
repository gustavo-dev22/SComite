using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Actualizar_Horario_Logs_Peru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Actualizar SP de Registro para insertar la hora local de Perú (UTC-5)
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

                    -- Hora exacta de Perú (UTC - 5 Horas)
                    DECLARE @FechaPeru DATETIME2 = DATEADD(HOUR, -5, GETUTCDATE());

                    INSERT INTO LogsSistema (Fecha, Nivel, Modulo, Accion, Usuario, IP, Mensaje, DetalleException)
                    VALUES (@FechaPeru, @Nivel, @Modulo, @Accion, @Usuario, @IP, @Mensaje, @DetalleException);

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ");

            // 2. Actualizar SP de Filtros para comparar contra la hora local de Perú
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

                    -- Fecha de hoy en Perú
                    DECLARE @HoyPeru DATETIME2 = DATEADD(HOUR, -5, GETUTCDATE());

                    -- Ajustar rango por defecto si viene nulo
                    IF @FechaInicio IS NULL SET @FechaInicio = DATEADD(DAY, -30, @HoyPeru);
                    
                    -- Si @FechaFin viene solo como YYYY-MM-DD, llevarlo al final del día (23:59:59)
                    IF @FechaFin IS NULL 
                        SET @FechaFin = @HoyPeru;
                    ELSE 
                        SET @FechaFin = DATEADD(SECOND, -1, DATEADD(DAY, 1, CAST(CAST(@FechaFin AS DATE) AS DATETIME2)));

                    DECLARE @Skip INT = (@Pagina - 1) * @TamanoPagina;

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
            // Revertir a GETUTCDATE() si se deshace la migración
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
        }
    }
}
