using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaActualizacionActas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Agregar columnas de auditoría de actualización
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ActasAsambleaComite') AND name = 'UsuarioActualizacion')
                BEGIN
                    ALTER TABLE ActasAsambleaComite ADD UsuarioActualizacion VARCHAR(100) NULL;
                    ALTER TABLE ActasAsambleaComite ADD FechaActualizacion DATETIME2 NULL;
                END
            ");

            // 2. Actualizar SP Listar para incluir campos de auditoría
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
                        a.UsuarioActualizacion,
                        a.FechaActualizacion,
                        a.Estado
                    FROM ActasAsambleaComite a
                    WHERE a.AulaId = @AulaId
                      AND YEAR(a.FechaReunion) = @AnioLectivo
                      AND a.Estado = 1
                    ORDER BY a.FechaReunion DESC, a.Id DESC;
                END
            ");

            // 3. Actualizar SP Guardar para preservar Registro original y grabar Actualización
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
                    @UsuarioAccion VARCHAR(100)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Id = 0
                    BEGIN
                        -- 🚀 CREACIÓN: Graba UsuarioRegistro y FechaRegistro. No toca Actualizacion.
                        INSERT INTO ActasAsambleaComite (
                            AulaId, 
                            NumeroActa, 
                            Titulo, 
                            FechaReunion, 
                            AgendaAcuerdos, 
                            EstadoActa, 
                            UrlDocumentoPdf, 
                            UsuarioRegistro, 
                            FechaRegistro
                        )
                        VALUES (
                            @AulaId, 
                            UPPER(@NumeroActa), 
                            @Titulo, 
                            @FechaReunion, 
                            @AgendaAcuerdos, 
                            UPPER(@EstadoActa), 
                            @UrlDocumentoPdf, 
                            @UsuarioAccion, 
                            DATEADD(HOUR, -5, GETUTCDATE())
                        );

                        SELECT CAST(SCOPE_IDENTITY() AS INT);
                    END
                    ELSE
                    BEGIN
                        -- 🚀 EDICIÓN: NO modifica UsuarioRegistro ni FechaRegistro. Graba UsuarioActualizacion y FechaActualizacion.
                        UPDATE ActasAsambleaComite
                        SET NumeroActa = UPPER(@NumeroActa),
                            Titulo = @Titulo,
                            FechaReunion = @FechaReunion,
                            AgendaAcuerdos = @AgendaAcuerdos,
                            EstadoActa = UPPER(@EstadoActa),
                            UrlDocumentoPdf = @UrlDocumentoPdf,
                            UsuarioActualizacion = @UsuarioAccion,
                            FechaActualizacion = DATEADD(HOUR, -5, GETUTCDATE())
                        WHERE Id = @Id AND AulaId = @AulaId;

                        SELECT @Id;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
