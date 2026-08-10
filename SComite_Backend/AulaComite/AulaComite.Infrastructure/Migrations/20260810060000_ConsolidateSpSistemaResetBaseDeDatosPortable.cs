using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateSpSistemaResetBaseDeDatosPortable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // M18: Consolidación de la versión FINAL de sp_Sistema_ResetBaseDeDatos en una
            // única migración de producción limpia (sustituye a los cambios previos repetitivos
            // del mismo SP) y reemplaza sp_MSforeachtable por T-SQL PORTABLE (sys.tables +
            // sp_executesql), compatible con SQL Server Standard/Express y Azure SQL.
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Sistema_ResetBaseDeDatos]
                    @RutaBackupFolder VARCHAR(500) = 'C:\Backups_AulaComite\'
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- 1. CREAR RUTA Y ARCHIVO DE BACKUP PRE-PURGA AUTOMÁTICO
                    DECLARE @NombreBaseDatos sysname;
                    DECLARE @NombreBackup VARCHAR(255);
                    DECLARE @RutaCompletaBackup VARCHAR(750);
                    DECLARE @FechaHoraStr VARCHAR(50);

                    SET @NombreBaseDatos = DB_NAME();
                    SET @FechaHoraStr = REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, DATEADD(HOUR, -5, GETUTCDATE()), 120), '-', ''), ':', ''), ' ', '_');
                    SET @NombreBackup = 'PrePurga_' + @NombreBaseDatos + '_' + @FechaHoraStr + '.bak';
                    SET @RutaCompletaBackup = @RutaBackupFolder + @NombreBackup;

                    BEGIN TRY
                        -- Backup físico automático antes de vaciar las tablas
                        BACKUP DATABASE @NombreBaseDatos
                        TO DISK = @RutaCompletaBackup
                        WITH FORMAT, INIT, NAME = 'Backup Pre Purga Automático', SKIP, NOUNLOAD, STATS = 10;
                    END TRY
                    BEGIN CATCH
                        THROW 50001, 'No se pudo generar el backup de seguridad previo. La purga ha sido cancelada.', 1;
                        RETURN;
                    END CATCH

                    -- 2. EJECUTAR PURGA DE TABLAS REALES (EXCEPTUANDO InstitucionEducativa)
                    BEGIN TRANSACTION;
                    BEGIN TRY
                        -- Deshabilitar temporalmente todas las Foreign Keys (PORTABLE, sin sp_MSforeachtable)
                        DECLARE @CmdDeshabilitar NVARCHAR(MAX) = N'';
                        SELECT @CmdDeshabilitar = @CmdDeshabilitar + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(t.object_id)) + N'.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;' + CHAR(13) + CHAR(10)
                        FROM sys.tables t
                        WHERE t.is_ms_shipped = 0;
                        EXEC sp_executesql @CmdDeshabilitar;

                        DELETE FROM AnuncioLecturasEstudiante;
                        DELETE FROM AnunciosComite;
                        DELETE FROM ActasAsambleaComite;
                        DELETE FROM DonacionesComite;
                        DELETE FROM GastosComite;
                        DELETE FROM CuotaDetalleEstudiante;
                        DELETE FROM Cuotas;
                        DELETE FROM ActividadesComite;
                        DELETE FROM ComiteIntegrantes;
                        DELETE FROM Estudiantes;
                        DELETE FROM Aulas;
                        DELETE FROM PeriodosLectivos;
                        DELETE FROM LogsSistema;

                        -- Reiniciar contadores AUTO-INCREMENTALES (IDENTITY) a 0
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AnuncioLecturasEstudiante') DBCC CHECKIDENT ('AnuncioLecturasEstudiante', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AnunciosComite') DBCC CHECKIDENT ('AnunciosComite', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ActasAsambleaComite') DBCC CHECKIDENT ('ActasAsambleaComite', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'DonacionesComite') DBCC CHECKIDENT ('DonacionesComite', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GastosComite') DBCC CHECKIDENT ('GastosComite', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CuotaDetalleEstudiante') DBCC CHECKIDENT ('CuotaDetalleEstudiante', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Cuotas') DBCC CHECKIDENT ('Cuotas', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ActividadesComite') DBCC CHECKIDENT ('ActividadesComite', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ComiteIntegrantes') DBCC CHECKIDENT ('ComiteIntegrantes', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Estudiantes') DBCC CHECKIDENT ('Estudiantes', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Aulas') DBCC CHECKIDENT ('Aulas', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PeriodosLectivos') DBCC CHECKIDENT ('PeriodosLectivos', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'LogsSistema') DBCC CHECKIDENT ('LogsSistema', RESEED, 0);

                        -- Re-habilitar y validar todas las Foreign Keys (PORTABLE)
                        DECLARE @CmdHabilitar NVARCHAR(MAX) = N'';
                        SELECT @CmdHabilitar = @CmdHabilitar + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(t.object_id)) + N'.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13) + CHAR(10)
                        FROM sys.tables t
                        WHERE t.is_ms_shipped = 0
                          AND EXISTS (SELECT 1 FROM sys.foreign_keys fk WHERE fk.parent_object_id = t.object_id OR fk.referenced_object_id = t.object_id);
                        EXEC sp_executesql @CmdHabilitar;

                        COMMIT TRANSACTION;
                        SELECT 1 AS Exitoso, @RutaCompletaBackup AS RutaBackup;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                        -- Re-habilitar las Foreign Keys en caso de error (PORTABLE)
                        DECLARE @CmdHabilitarCatch NVARCHAR(MAX) = N'';
                        SELECT @CmdHabilitarCatch = @CmdHabilitarCatch + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(t.object_id)) + N'.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13) + CHAR(10)
                        FROM sys.tables t
                        WHERE t.is_ms_shipped = 0
                          AND EXISTS (SELECT 1 FROM sys.foreign_keys fk WHERE fk.parent_object_id = t.object_id OR fk.referenced_object_id = t.object_id);
                        EXEC sp_executesql @CmdHabilitarCatch;
                        THROW;
                    END CATCH;
                END
            ", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_Sistema_ResetBaseDeDatos];", suppressTransaction: true);
        }
    }
}