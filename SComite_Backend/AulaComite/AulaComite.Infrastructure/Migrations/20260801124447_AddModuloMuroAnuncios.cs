using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloMuroAnuncios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear Tabla AnunciosComite
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AnunciosComite')
                BEGIN
                    CREATE TABLE AnunciosComite (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        AulaId INT NOT NULL,
                        Titulo NVARCHAR(150) NOT NULL,
                        Contenido NVARCHAR(MAX) NOT NULL,
                        Categoria VARCHAR(30) NOT NULL DEFAULT 'INFORMATIVO', -- URGENTE, CITACION, TESORERIA, EVENTO, INFORMATIVO
                        EsFijado BIT NOT NULL DEFAULT 0,
                        UrlAdjunto NVARCHAR(500) NULL,
                        UsuarioRegistro VARCHAR(100) NOT NULL,
                        FechaPublicacion DATETIME2 NOT NULL DEFAULT (DATEADD(HOUR, -5, GETUTCDATE())),
                        CantidadVistas INT NOT NULL DEFAULT 0,
                        Estado BIT NOT NULL DEFAULT 1,
                        FOREIGN KEY (AulaId) REFERENCES Aulas(Id) ON DELETE CASCADE
                    );

                    CREATE INDEX IX_AnunciosComite_AulaId ON AnunciosComite(AulaId);
                    CREATE INDEX IX_AnunciosComite_Fecha ON AnunciosComite(FechaPublicacion);
                END
            ");

            // 2. SP: Listar Anuncios por Aula
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Anuncios_ListarPorAula
                    @AulaId INT,
                    @AnioLectivo INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        a.Id,
                        a.AulaId,
                        a.Titulo,
                        a.Contenido,
                        a.Categoria,
                        a.EsFijado,
                        a.UrlAdjunto,
                        a.UsuarioRegistro,
                        a.FechaPublicacion,
                        a.CantidadVistas,
                        a.Estado
                    FROM AnunciosComite a
                    WHERE a.AulaId = @AulaId
                      AND YEAR(a.FechaPublicacion) = @AnioLectivo
                      AND a.Estado = 1
                    ORDER BY a.EsFijado DESC, a.FechaPublicacion DESC;
                END
            ");

            // 3. SP: Guardar / Editar Anuncio
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Anuncios_Guardar
                    @Id INT = 0,
                    @AulaId INT,
                    @Titulo NVARCHAR(150),
                    @Contenido NVARCHAR(MAX),
                    @Categoria VARCHAR(30),
                    @EsFijado BIT = 0,
                    @UrlAdjunto NVARCHAR(500) = NULL,
                    @UsuarioRegistro VARCHAR(100)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Id = 0
                    BEGIN
                        INSERT INTO AnunciosComite (AulaId, Titulo, Contenido, Categoria, EsFijado, UrlAdjunto, UsuarioRegistro)
                        VALUES (@AulaId, @Titulo, @Contenido, UPPER(@Categoria), @EsFijado, @UrlAdjunto, @UsuarioRegistro);

                        SELECT CAST(SCOPE_IDENTITY() AS INT);
                    END
                    ELSE
                    BEGIN
                        UPDATE AnunciosComite
                        SET Titulo = @Titulo,
                            Contenido = @Contenido,
                            Categoria = UPPER(@Categoria),
                            EsFijado = @EsFijado,
                            UrlAdjunto = @UrlAdjunto
                        WHERE Id = @Id AND AulaId = @AulaId;

                        SELECT @Id;
                    END
                END
            ");

            // 4. SP: Eliminar / Desactivar Anuncio
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Anuncios_Eliminar
                    @Id INT,
                    @AulaId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE AnunciosComite SET Estado = 0 WHERE Id = @Id AND AulaId = @AulaId;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Anuncios_Eliminar;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Anuncios_Guardar;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_Anuncios_ListarPorAula;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS AnunciosComite;");
        }
    }
}
