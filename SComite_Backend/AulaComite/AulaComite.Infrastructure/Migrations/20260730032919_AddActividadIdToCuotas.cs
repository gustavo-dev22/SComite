using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActividadIdToCuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Agregar columna ActividadId y FK con ON DELETE NO ACTION para evitar caminos múltiples
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID('Cuotas') AND name = 'ActividadId'
                )
                BEGIN
                    ALTER TABLE Cuotas ADD ActividadId INT NULL;

                    ALTER TABLE Cuotas
                    ADD CONSTRAINT FK_Cuotas_ActividadesComite_ActividadId
                    FOREIGN KEY (ActividadId) REFERENCES ActividadesComite(Id)
                    ON DELETE NO ACTION;

                    CREATE INDEX IX_Cuotas_ActividadId ON Cuotas(ActividadId);
                END
            ");

            // 2. Actualizar el Stored Procedure sp_Cuotas_Crear para soportar @ActividadId
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Cuotas_Crear]
                    @AulaId INT,
                    @Concepto VARCHAR(150),
                    @MontoIndividual DECIMAL(10,2),
                    @FechaVencimiento DATE,
                    @Observacion NVARCHAR(500) = NULL,
                    @ActividadId INT = NULL -- 🚀 Parámetro opcional
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;
                    BEGIN TRY
                        -- A. Insertar Cabecera de Cuota incluyendo ActividadId
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
            // Revertir el SP a la versión anterior sin @ActividadId
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Cuotas_Crear]
                    @AulaId INT,
                    @Concepto VARCHAR(150),
                    @MontoIndividual DECIMAL(10,2),
                    @FechaVencimiento DATE,
                    @Observacion NVARCHAR(500) = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    BEGIN TRANSACTION;
                    BEGIN TRY
                        INSERT INTO Cuotas (AulaId, Concepto, MontoIndividual, FechaVencimiento, Observacion)
                        VALUES (@AulaId, @Concepto, @MontoIndividual, @FechaVencimiento, @Observacion);

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

            // Eliminar columna y FK
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID('Cuotas') AND name = 'ActividadId'
                )
                BEGIN
                    ALTER TABLE Cuotas DROP CONSTRAINT FK_Cuotas_ActividadesComite_ActividadId;
                    DROP INDEX IF EXISTS IX_Cuotas_ActividadId ON Cuotas;
                    ALTER TABLE Cuotas DROP COLUMN ActividadId;
                END
            ");
        }
    }
}
