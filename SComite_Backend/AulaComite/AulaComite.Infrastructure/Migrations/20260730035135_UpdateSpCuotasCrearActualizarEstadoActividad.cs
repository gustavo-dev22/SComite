using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpCuotasCrearActualizarEstadoActividad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Cuotas_Crear]
                    @AulaId INT,
                    @Concepto VARCHAR(150),
                    @MontoIndividual DECIMAL(10,2),
                    @FechaVencimiento DATE,
                    @Observacion NVARCHAR(500) = NULL,
                    @ActividadId INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;
                    BEGIN TRY
                        -- A. Insertar Cabecera de Cuota
                        INSERT INTO Cuotas (AulaId, Concepto, MontoIndividual, FechaVencimiento, Observacion, ActividadId)
                        VALUES (@AulaId, @Concepto, @MontoIndividual, @FechaVencimiento, @Observacion, @ActividadId);

                        DECLARE @NuevoCuotaId INT = SCOPE_IDENTITY();

                        -- B. Insertar Detalle Masivo a todos los estudiantes activos del Aula
                        INSERT INTO CuotaDetalleEstudiante (CuotaId, EstudianteId, MontoAsignado, MontoPagado, EstadoPago)
                        SELECT 
                            @NuevoCuotaId, 
                            Id, 
                            @MontoIndividual, 
                            0.00, 
                            'PENDIENTE'
                        FROM Estudiantes
                        WHERE AulaId = @AulaId AND Estado = 1;

                        -- 🚀 C. Actualizar estado de la Actividad a 'EN_PROCESO' si proviene de una Actividad del Cronograma
                        IF @ActividadId IS NOT NULL AND @ActividadId > 0
                        BEGIN
                            UPDATE ActividadesComite
                            SET Estado = 'EN_PROCESO'
                            WHERE Id = @ActividadId AND Estado = 'PLANIFICADA';
                        END

                        COMMIT TRANSACTION;
                        SELECT @NuevoCuotaId;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Cuotas_Crear]
                    @AulaId INT,
                    @Concepto VARCHAR(150),
                    @MontoIndividual DECIMAL(10,2),
                    @FechaVencimiento DATE,
                    @Observacion NVARCHAR(500) = NULL,
                    @ActividadId INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;
                    BEGIN TRY
                        INSERT INTO Cuotas (AulaId, Concepto, MontoIndividual, FechaVencimiento, Observacion, ActividadId)
                        VALUES (@AulaId, @Concepto, @MontoIndividual, @FechaVencimiento, @Observacion, @ActividadId);

                        DECLARE @NuevoCuotaId INT = SCOPE_IDENTITY();

                        INSERT INTO CuotaDetalleEstudiante (CuotaId, EstudianteId, MontoAsignado, MontoPagado, EstadoPago)
                        SELECT 
                            @NuevoCuotaId, 
                            Id, 
                            @MontoIndividual, 
                            0.00, 
                            'PENDIENTE'
                        FROM Estudiantes
                        WHERE AulaId = @AulaId AND Estado = 1;

                        COMMIT TRANSACTION;
                        SELECT @NuevoCuotaId;
                    END TRY
                    BEGIN CATCH
                        ROLLBACK TRANSACTION;
                        THROW;
                    END CATCH;
                END
            ");
        }
    }
}
