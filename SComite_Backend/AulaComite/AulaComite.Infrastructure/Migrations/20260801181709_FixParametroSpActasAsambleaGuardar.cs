using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixParametroSpActasAsambleaGuardar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    @UsuarioRegistro VARCHAR(100) -- 🚀 Homologado exactamente con Dapper
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Id = 0
                    BEGIN
                        -- CREACIÓN: Graba UsuarioRegistro y FechaRegistro
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
                            @UsuarioRegistro, 
                            DATEADD(HOUR, -5, GETUTCDATE())
                        );

                        SELECT CAST(SCOPE_IDENTITY() AS INT);
                    END
                    ELSE
                    BEGIN
                        -- EDICIÓN: Preserva UsuarioRegistro original y actualiza UsuarioActualizacion
                        UPDATE ActasAsambleaComite
                        SET NumeroActa = UPPER(@NumeroActa),
                            Titulo = @Titulo,
                            FechaReunion = @FechaReunion,
                            AgendaAcuerdos = @AgendaAcuerdos,
                            EstadoActa = UPPER(@EstadoActa),
                            UrlDocumentoPdf = @UrlDocumentoPdf,
                            UsuarioActualizacion = @UsuarioRegistro,
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
