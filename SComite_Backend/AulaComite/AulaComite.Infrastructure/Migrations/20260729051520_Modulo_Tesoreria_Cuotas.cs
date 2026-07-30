using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Modulo_Tesoreria_Cuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear Tabla Cuotas
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Cuotas')
                BEGIN
                    CREATE TABLE Cuotas (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        AulaId INT NOT NULL,
                        Concepto VARCHAR(150) NOT NULL,
                        MontoIndividual DECIMAL(10,2) NOT NULL,
                        FechaVencimiento DATE NOT NULL,
                        Estado VARCHAR(20) NOT NULL DEFAULT 'EN COBRO', -- EN COBRO, CERRADA, ANULADA
                        Observacion NVARCHAR(500) NULL,
                        FechaCreacion DATETIME2 NOT NULL DEFAULT (DATEADD(HOUR, -5, GETUTCDATE())),
                        FOREIGN KEY (AulaId) REFERENCES Aulas(Id) ON DELETE CASCADE
                    );

                    CREATE INDEX IX_Cuotas_AulaId ON Cuotas(AulaId);
                    CREATE INDEX IX_Cuotas_Estado ON Cuotas(Estado);
                END
            ");

            // 2. Crear Tabla CuotaDetalleEstudiante
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CuotaDetalleEstudiante')
                BEGIN
                    CREATE TABLE CuotaDetalleEstudiante (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        CuotaId INT NOT NULL,
                        EstudianteId INT NOT NULL,
                        MontoAsignado DECIMAL(10,2) NOT NULL,
                        MontoPagado DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                        EstadoPago VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE', -- PENDIENTE, PARCIAL, COMPLETO
                        FechaUltimoPago DATETIME2 NULL,
                        FOREIGN KEY (CuotaId) REFERENCES Cuotas(Id) ON DELETE CASCADE,
                        FOREIGN KEY (EstudianteId) REFERENCES Estudiantes(Id)
                    );

                    CREATE INDEX IX_CuotaDetalle_CuotaId ON CuotaDetalleEstudiante(CuotaId);
                    CREATE INDEX IX_CuotaDetalle_EstudianteId ON CuotaDetalleEstudiante(EstudianteId);
                END
            ");

            // 3. SP: Crear Cuota y Asignar masivamente a los Estudiantes del Aula
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_Crear
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
                        -- A. Insertar Cabecera de Cuota
                        INSERT INTO Cuotas (AulaId, Concepto, MontoIndividual, FechaVencimiento, Observacion)
                        VALUES (@AulaId, @Concepto, @MontoIndividual, @FechaVencimiento, @Observacion);

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

            // 4. SP: Obtener Cuotas de un Aula con Resumen Financiero
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Cuotas_ObtenerPorAula
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        c.Id,
                        c.AulaId,
                        c.Concepto,
                        c.MontoIndividual,
                        c.FechaVencimiento,
                        c.Estado,
                        c.Observacion,
                        c.FechaCreacion,
                        COUNT(cd.Id) AS TotalEstudiantesAsignados,
                        SUM(cd.MontoAsignado) AS TotalMontoEsperado,
                        SUM(cd.MontoPagado) AS TotalMontoRecaudado,
                        SUM(CASE WHEN cd.EstadoPago = 'COMPLETO' THEN 1 ELSE 0 END) AS EstudiantesAlDia,
                        SUM(CASE WHEN cd.EstadoPago = 'PENDIENTE' THEN 1 ELSE 0 END) AS EstudiantesPendientes
                    FROM Cuotas c
                    LEFT JOIN CuotaDetalleEstudiante cd ON c.Id = cd.CuotaId
                    WHERE c.AulaId = @AulaId
                    GROUP BY c.Id, c.AulaId, c.Concepto, c.MontoIndividual, c.FechaVencimiento, c.Estado, c.Observacion, c.FechaCreacion
                    ORDER BY c.FechaCreacion DESC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Cuotas_ObtenerPorAula");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Cuotas_Crear");
            migrationBuilder.Sql("DROP TABLE IF EXISTS CuotaDetalleEstudiante");
            migrationBuilder.Sql("DROP TABLE IF EXISTS Cuotas");
        }
    }
}
