using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedDataInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Deshabilitar temporalmente la verificación de llaves foráneas de forma PORTABLE
                -- (equivalente a sp_MSforeachtable, que no está disponible en todas las ediciones SQL).
                DECLARE @CmdDeshabilitar NVARCHAR(MAX) = N'';
                SELECT @CmdDeshabilitar = @CmdDeshabilitar + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(t.object_id)) + N'.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;' + CHAR(13) + CHAR(10)
                FROM sys.tables t
                WHERE t.is_ms_shipped = 0;
                EXEC sp_executesql @CmdDeshabilitar;

                -- 1. Vaciar todas las tablas dependientes primero
                DELETE FROM CuotaDetalleEstudiante;
                DELETE FROM GastosComite;
                DELETE FROM Cuotas;
                DELETE FROM ActividadesComite;
                DELETE FROM ComiteIntegrantes;
                DELETE FROM Estudiantes;

                -- 2. Eliminar las Aulas y PeriodosLectivos iniciales
                DELETE FROM Aulas;
                DELETE FROM PeriodosLectivos;

                -- 3. Reiniciar contadores de Identity
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CuotaDetalleEstudiante') DBCC CHECKIDENT ('CuotaDetalleEstudiante', RESEED, 0);
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GastosComite') DBCC CHECKIDENT ('GastosComite', RESEED, 0);
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Cuotas') DBCC CHECKIDENT ('Cuotas', RESEED, 0);
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ActividadesComite') DBCC CHECKIDENT ('ActividadesComite', RESEED, 0);
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ComiteIntegrantes') DBCC CHECKIDENT ('ComiteIntegrantes', RESEED, 0);
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Estudiantes') DBCC CHECKIDENT ('Estudiantes', RESEED, 0);
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Aulas') DBCC CHECKIDENT ('Aulas', RESEED, 0);
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PeriodosLectivos') DBCC CHECKIDENT ('PeriodosLectivos', RESEED, 0);

                -- 4. Volver a habilitar y validar todas las llaves foráneas de forma PORTABLE
                DECLARE @CmdHabilitar NVARCHAR(MAX) = N'';
                SELECT @CmdHabilitar = @CmdHabilitar + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(t.object_id)) + N'.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13) + CHAR(10)
                FROM sys.tables t
                WHERE t.is_ms_shipped = 0
                  AND EXISTS (SELECT 1 FROM sys.foreign_keys fk WHERE fk.parent_object_id = t.object_id OR fk.referenced_object_id = t.object_id);
                EXEC sp_executesql @CmdHabilitar;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
