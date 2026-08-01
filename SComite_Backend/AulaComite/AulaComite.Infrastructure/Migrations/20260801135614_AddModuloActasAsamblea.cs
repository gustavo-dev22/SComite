using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloActasAsamblea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear Tabla ActasAsambleaComite
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ActasAsambleaComite')
                BEGIN
                    CREATE TABLE ActasAsambleaComite (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        AulaId INT NOT NULL,
                        NumeroActa VARCHAR(20) NOT NULL, -- Ej: ACTA-001-2026
                        Titulo NVARCHAR(200) NOT NULL,
                        FechaReunion DATE NOT NULL,
                        AgendaAcuerdos NVARCHAR(MAX) NOT NULL,
                        EstadoActa VARCHAR(30) NOT NULL DEFAULT 'APROBADA', -- APROBADA, BORRADOR, EN_FIRMAS
                        UrlDocumentoPdf NVARCHAR(500) NULL, -- Enlace al PDF o escaneado con firmas
                        UsuarioRegistro VARCHAR(100) NOT NULL,
                        FechaRegistro DATETIME2 NOT NULL DEFAULT (DATEADD(HOUR, -5, GETUTCDATE())),
                        Estado BIT NOT NULL DEFAULT 1,
                        FOREIGN KEY (AulaId) REFERENCES Aulas(Id) ON DELETE CASCADE
                    );

                    CREATE INDEX IX_ActasAsamblea_AulaId ON ActasAsambleaComite(AulaId);
                    CREATE INDEX IX_ActasAsamblea_Fecha ON ActasAsambleaComite(FechaReunion);
                END
            ");

            // 2. SP: Listar Actas por Aula
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_ActasAsamblea_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
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
                        a.Estado
                    FROM ActasAsambleaComite a
                    WHERE a.AulaId = @AulaId
                      AND YEAR(a.FechaReunion) = @AnioLectivo
                      AND a.Estado = 1
                    ORDER BY a.FechaReunion DESC;
                END
            ");

            // 3. SP: Guardar / Editar Acta
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_ActasAsamblea_Guardar
                    @Id INT = 0,
                    @AulaId INT,
                    @NumeroActa VARCHAR(20),
                    @Titulo NVARCHAR(200),
                    @FechaReunion DATE,
                    @AgendaAcuerdos NVARCHAR(MAX),
                    @EstadoActa VARCHAR(30),
                    @UrlDocumentoPdf NVARCHAR(500) = NULL,
                    @UsuarioRegistro VARCHAR(100)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Id = 0
                    BEGIN
                        INSERT INTO ActasAsambleaComite (AulaId, NumeroActa, Titulo, FechaReunion, AgendaAcuerdos, EstadoActa, UrlDocumentoPdf, UsuarioRegistro)
                        VALUES (@AulaId, UPPER(@NumeroActa), @Titulo, @FechaReunion, @AgendaAcuerdos, UPPER(@EstadoActa), @UrlDocumentoPdf, @UsuarioRegistro);

                        SELECT CAST(SCOPE_IDENTITY() AS INT);
                    END
                    ELSE
                    BEGIN
                        UPDATE ActasAsambleaComite
                        SET NumeroActa = UPPER(@NumeroActa),
                            Titulo = @Titulo,
                            FechaReunion = @FechaReunion,
                            AgendaAcuerdos = @AgendaAcuerdos,
                            EstadoActa = UPPER(@EstadoActa),
                            UrlDocumentoPdf = @UrlDocumentoPdf,
                            UsuarioRegistro = @UsuarioRegistro
                        WHERE Id = @Id AND AulaId = @AulaId;

                        SELECT @Id;
                    END
                END
            ");

            // 4. SP: Eliminar / Desactivar Acta
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_ActasAsamblea_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE ActasAsambleaComite SET Estado = 0 WHERE Id = @Id AND AulaId = @AulaId;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ActasAsamblea_Eliminar;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ActasAsamblea_Guardar;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ActasAsamblea_ListarPorAula;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ActasAsambleaComite;");
        }
    }
}
