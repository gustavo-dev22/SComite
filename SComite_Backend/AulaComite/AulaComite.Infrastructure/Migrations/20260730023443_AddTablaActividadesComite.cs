using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTablaActividadesComite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear Tabla ActividadesComite con verificación de existencia
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ActividadesComite')
                BEGIN
                    CREATE TABLE ActividadesComite (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        AulaId INT NOT NULL,
                        NombreActividad VARCHAR(150) NOT NULL,
                        Descripcion NVARCHAR(500) NULL,
                        FechaProgramada DATE NOT NULL,
                        MontoPresupuestado DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                        CuotaSugeridaPorAlumno DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                        Estado VARCHAR(20) NOT NULL DEFAULT 'PLANIFICADA', -- PLANIFICADA, EN_PROCESO, FINALIZADA, CANCELADA
                        FechaRegistro DATETIME2 NOT NULL DEFAULT (DATEADD(HOUR, -5, GETUTCDATE())),
                        FOREIGN KEY (AulaId) REFERENCES Aulas(Id) ON DELETE CASCADE
                    );

                    CREATE INDEX IX_ActividadesComite_AulaId ON ActividadesComite(AulaId);
                    CREATE INDEX IX_ActividadesComite_Estado ON ActividadesComite(Estado);
                END
            ");

            // 2. SP: Listar Actividades por Aula y Año Lectivo
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Actividades_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        a.Id,
                        a.AulaId,
                        a.NombreActividad,
                        a.Descripcion,
                        a.FechaProgramada,
                        a.MontoPresupuestado,
                        a.CuotaSugeridaPorAlumno,
                        a.Estado,
                        a.FechaRegistro
                    FROM ActividadesComite a
                    INNER JOIN Aulas au ON a.AulaId = au.Id
                    INNER JOIN PeriodosLectivos p ON au.Id = p.Id
                    WHERE a.AulaId = @AulaId
                      AND p.Anio = @AnioLectivo
                    ORDER BY a.FechaProgramada ASC;
                END
            ");

            // 3. SP: Crear o Editar Actividad
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Actividades_Guardar
                    @Id INT = 0,
                    @AulaId INT,
                    @NombreActividad VARCHAR(150),
                    @Descripcion NVARCHAR(500) = NULL,
                    @FechaProgramada DATE,
                    @MontoPresupuestado DECIMAL(10,2),
                    @CuotaSugeridaPorAlumno DECIMAL(10,2),
                    @Estado VARCHAR(20)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Id = 0
                    BEGIN
                        INSERT INTO ActividadesComite (
                            AulaId, 
                            NombreActividad, 
                            Descripcion, 
                            FechaProgramada, 
                            MontoPresupuestado, 
                            CuotaSugeridaPorAlumno, 
                            Estado
                        )
                        VALUES (
                            @AulaId, 
                            @NombreActividad, 
                            @Descripcion, 
                            @FechaProgramada, 
                            @MontoPresupuestado, 
                            @CuotaSugeridaPorAlumno, 
                            @Estado
                        );
                        
                        SELECT CAST(SCOPE_IDENTITY() AS INT);
                    END
                    ELSE
                    BEGIN
                        UPDATE ActividadesComite
                        SET NombreActividad = @NombreActividad,
                            Descripcion = @Descripcion,
                            FechaProgramada = @FechaProgramada,
                            MontoPresupuestado = @MontoPresupuestado,
                            CuotaSugeridaPorAlumno = @CuotaSugeridaPorAlumno,
                            Estado = @Estado
                        WHERE Id = @Id AND AulaId = @AulaId;

                        SELECT @Id;
                    END
                END
            ");

            // 4. SP: Eliminar Actividad
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Actividades_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DELETE FROM ActividadesComite WHERE Id = @Id AND AulaId = @AulaId;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Actividades_Eliminar;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Actividades_Guardar;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Actividades_ListarPorAula;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ActividadesComite;");
        }
    }
}
