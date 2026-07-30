using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Agregar_Cuotas_Mensuales_Recurrentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Agregar columnas a la tabla Cuotas
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Cuotas') AND name = 'TipoCuota')
                BEGIN
                    ALTER TABLE Cuotas ADD TipoCuota VARCHAR(30) NOT NULL DEFAULT 'EXTRAORDINARIA'; -- EXTRAORDINARIA, RECURRENTE_MENSUAL
                    ALTER TABLE Cuotas ADD MesCorrespondiente INT NULL; -- 3 = Marzo, 4 = Abril, ... 12 = Diciembre
                END
            ");

            // 2. Stored Procedure para Generar Programación Mensual de Caja Chica
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_GenerarProgramacionMensual
                    @AulaId INT,
                    @ConceptoBase VARCHAR(100), -- Ej. 'Caja Chica Mensual'
                    @MontoMensual DECIMAL(10,2),
                    @DiaVencimiento INT = 10, -- Día del mes límite (ej. día 10 de cada mes)
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Meses del año escolar: Marzo (3) a Diciembre (12)
                    DECLARE @Mes INT = 3;
                    DECLARE @FechaVencimiento DATE;
                    DECLARE @ConceptoMes VARCHAR(150);
                    DECLARE @NuevaCuotaId INT;

                    WHILE @Mes <= 12
                    BEGIN
                        -- Calcular fecha de vencimiento para el mes (Año-Mes-DíaVencimiento)
                        SET @FechaVencimiento = DATEFROMPARTS(@AnioLectivo, @Mes, @DiaVencimiento);

                        -- Nombre del mes en español
                        DECLARE @NombreMes VARCHAR(20) = CASE @Mes
                            WHEN 3 THEN 'Marzo' WHEN 4 THEN 'Abril' WHEN 5 THEN 'Mayo'
                            WHEN 6 THEN 'Junio' WHEN 7 THEN 'Julio' WHEN 8 THEN 'Agosto'
                            WHEN 9 THEN 'Setiembre' WHEN 10 THEN 'Octubre' WHEN 11 THEN 'Noviembre'
                            WHEN 12 THEN 'Diciembre'
                        END;

                        SET @ConceptoMes = @ConceptoBase + ' - ' + @NombreMes + ' ' + CAST(@AnioLectivo AS VARCHAR(4));

                        -- Insertar la cabecera si no se creó previamente para el mismo mes
                        IF NOT EXISTS (SELECT 1 FROM Cuotas WHERE AulaId = @AulaId AND TipoCuota = 'RECURRENTE_MENSUAL' AND MesCorrespondiente = @Mes)
                        BEGIN
                            INSERT INTO Cuotas (AulaId, Concepto, MontoIndividual, FechaVencimiento, Estado, Observacion, TipoCuota, MesCorrespondiente)
                            VALUES (@AulaId, @ConceptoMes, @MontoMensual, @FechaVencimiento, 'EN COBRO', 'Cuota mensual programada de caja chica', 'RECURRENTE_MENSUAL', @Mes);

                            SET @NuevaCuotaId = SCOPE_IDENTITY();

                            -- Asignar la cuota a todos los estudiantes activos del aula
                            INSERT INTO CuotaDetalleEstudiante (CuotaId, EstudianteId, MontoAsignado, MontoPagado, EstadoPago)
                            SELECT @NuevaCuotaId, Id, @MontoMensual, 0.00, 'PENDIENTE'
                            FROM Estudiantes
                            WHERE AulaId = @AulaId AND Estado = 1;
                        END

                        SET @Mes = @Mes + 1;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Cuotas_GenerarProgramacionMensual");
        }
    }
}
