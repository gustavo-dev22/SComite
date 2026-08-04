using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpSistemaResetBaseDeDatosConBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🚀 CRUCIAL: suppressTransaction: true permite que SQL Server ejecute el comando BACKUP DATABASE sin fallar por estar dentro de una transacción implícita
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Sistema_ResetBaseDeDatos]
                    @RutaBackupFolder VARCHAR(500) = 'C:\Backups_AulaComite\'
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- 1. CREAR RUTA Y ARCHIVO DE BACKUP PRE-PURGA AUTOMÁTICO
                    DECLARE @NombreBackup VARCHAR(255);
                    DECLARE @RutaCompletaBackup VARCHAR(750);
                    DECLARE @FechaHoraStr VARCHAR(50);

                    SET @FechaHoraStr = REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, DATEADD(HOUR, -5, GETUTCDATE()), 120), '-', ''), ':', ''), ' ', '_');
                    SET @NombreBackup = 'PrePurga_db_AulaComite_' + @FechaHoraStr + '.bak';
                    SET @RutaCompletaBackup = @RutaBackupFolder + @NombreBackup;

                    BEGIN TRY
                        -- Generar Backup físico automático antes de purgar
                        BACKUP DATABASE [db_AulaComite] 
                        TO DISK = @RutaCompletaBackup 
                        WITH FORMAT, INIT, NAME = 'Backup Pre Purga Automático', SKIP, NOUNLOAD, STATS = 10;
                    END TRY
                    BEGIN CATCH
                        -- Si el comando BACKUP de SQL Server falla (ej. permisos de carpeta), se aborta la purga por seguridad
                        THROW 50001, 'No se pudo generar el backup de seguridad previo. La purga ha sido cancelada.', 1;
                        RETURN;
                    END CATCH

                    -- 2. EJECUTAR PURGA DE TABLAS
                    BEGIN TRANSACTION;
                    BEGIN TRY
                        -- Deshabilitar temporalmente todas las Foreign Keys
                        EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

                        -- Eliminar registros de las tablas transaccionales (Conservando InstitucionEducativa)
                        DELETE FROM AnuncioLecturasEstudiante;
                        DELETE FROM AnunciosComite;
                        DELETE FROM ComunicadoComentarios;
                        DELETE FROM ComunicadoLecturas;
                        DELETE FROM Comunicados;
                        DELETE FROM ActasAsambleaComite;
                        DELETE FROM CronogramaActividades;
                        DELETE FROM CuotaDetalleEstudiante;
                        DELETE FROM GastosComite;
                        DELETE FROM Cuotas;
                        DELETE FROM ActividadesComite;
                        DELETE FROM ComiteIntegrantes;
                        DELETE FROM Estudiantes;
                        DELETE FROM Aulas;
                        DELETE FROM PeriodosLectivos;
                        DELETE FROM LogsSistema;

                        -- Reiniciar contadores IDENTITY a 0
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AnuncioLecturasEstudiante') DBCC CHECKIDENT ('AnuncioLecturasEstudiante', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AnunciosComite') DBCC CHECKIDENT ('AnunciosComite', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ActasAsambleaComite') DBCC CHECKIDENT ('ActasAsambleaComite', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CronogramaActividades') DBCC CHECKIDENT ('CronogramaActividades', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CuotaDetalleEstudiante') DBCC CHECKIDENT ('CuotaDetalleEstudiante', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GastosComite') DBCC CHECKIDENT ('GastosComite', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Cuotas') DBCC CHECKIDENT ('Cuotas', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ActividadesComite') DBCC CHECKIDENT ('ActividadesComite', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ComiteIntegrantes') DBCC CHECKIDENT ('ComiteIntegrantes', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Estudiantes') DBCC CHECKIDENT ('Estudiantes', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Aulas') DBCC CHECKIDENT ('Aulas', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PeriodosLectivos') DBCC CHECKIDENT ('PeriodosLectivos', RESEED, 0);
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'LogsSistema') DBCC CHECKIDENT ('LogsSistema', RESEED, 0);

                        -- Re-habilitar Foreign Keys
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
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_Sistema_ResetBaseDeDatos];", suppressTransaction: true);
        }
    }
}
