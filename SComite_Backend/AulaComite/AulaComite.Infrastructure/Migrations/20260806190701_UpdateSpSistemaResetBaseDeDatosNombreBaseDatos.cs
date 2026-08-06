using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpSistemaResetBaseDeDatosNombreBaseDatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🔧 Corrección: el SP anterior hacía `BACKUP DATABASE [db_AulaComite]`, pero la base
            // real se llama `db_ComiteAula`, por lo que el BACKUP fallaba (error 911) y el bloque
            // CATCH lanzaba el error 50001 cancelando la purga. Ahora se usa DB_NAME() para
            // respaldar SIEMPRE la base de datos actual, sin nombres hardcodeados.
            // suppressTransaction: true permite que el comando BACKUP DATABASE se ejecute sin fallar.
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
                        -- Generar Backup físico automático en SQL Server antes de vaciar las tablas
                        BACKUP DATABASE @NombreBaseDatos
                        TO DISK = @RutaCompletaBackup
                        WITH FORMAT, INIT, NAME = 'Backup Pre Purga Automático', SKIP, NOUNLOAD, STATS = 10;
                    END TRY
                    BEGIN CATCH
                        -- Si el comando BACKUP falla (ej. permisos de carpeta), se aborta la purga por seguridad
                        THROW 50001, 'No se pudo generar el backup de seguridad previo. La purga ha sido cancelada.', 1;
                        RETURN;
                    END CATCH

                    -- 2. EJECUTAR PURGA DE TABLAS REALES (EXCEPTUANDO InstitucionEducativa)
                    BEGIN TRANSACTION;
                    BEGIN TRY
                        -- Deshabilitar temporalmente todas las Foreign Keys de la BD
                        EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

                        -- Eliminar registros de las tablas transaccionales
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

                        -- Re-habilitar y validar todas las Foreign Keys
                        EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';

                        COMMIT TRANSACTION;
                        SELECT 1 AS Exitoso, @RutaCompletaBackup AS RutaBackup;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                        EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
                        THROW;
                    END CATCH;
                END
            ", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir al estado anterior (versión con nombre de base de datos hardcodeado)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Sistema_ResetBaseDeDatos]
                    @RutaBackupFolder VARCHAR(500) = 'C:\Backups_AulaComite\'
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @NombreBackup VARCHAR(255);
                    DECLARE @RutaCompletaBackup VARCHAR(750);
                    DECLARE @FechaHoraStr VARCHAR(50);

                    SET @FechaHoraStr = REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, DATEADD(HOUR, -5, GETUTCDATE()), 120), '-', ''), ':', ''), ' ', '_');
                    SET @NombreBackup = 'PrePurga_db_AulaComite_' + @FechaHoraStr + '.bak';
                    SET @RutaCompletaBackup = @RutaBackupFolder + @NombreBackup;

                    BEGIN TRY
                        BACKUP DATABASE [db_AulaComite]
                        TO DISK = @RutaCompletaBackup
                        WITH FORMAT, INIT, NAME = 'Backup Pre Purga Automático', SKIP, NOUNLOAD, STATS = 10;
                    END TRY
                    BEGIN CATCH
                        THROW 50001, 'No se pudo generar el backup de seguridad previo. La purga ha sido cancelada.', 1;
                        RETURN;
                    END CATCH

                    BEGIN TRANSACTION;
                    BEGIN TRY
                        EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

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

                        EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';

                        COMMIT TRANSACTION;
                        SELECT 1 AS Exitoso, @RutaCompletaBackup AS RutaBackup;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                        EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
                        THROW;
                    END CATCH;
                END
            ", suppressTransaction: true);
        }
    }
}
