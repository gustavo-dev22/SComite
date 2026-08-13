using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AulaComite.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Corrección de producción del problema "Id = 0 al insertar registros":
    ///   - sp_Estudiantes_Crear: SET NOCOUNT ON (evita que el ROWCOUNT del INSERT
    ///     interfiera con la lectura del escalar devuelto por ExecuteScalar).
    ///   - sp_Gastos_Registrar: SELECT CAST(SCOPE_IDENTITY() AS INT) (homologa el tipo
    ///     devuelto y garantiza que Dapper reciba el Id entero autogenerado).
    ///   - sp_Sistema_ResetBaseDeDatos: el parámetro de ruta de backup deja de tener el
    ///     valor fijo 'C:\Backups_AulaComite\' (la ruta ahora la inyecta la API con
    ///     AppDomain.CurrentDomain.BaseDirectory + "Backups").
    /// </summary>
    public partial class FixInsertIdentityScopeIdentityProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. sp_Estudiantes_Crear: SET NOCOUNT ON para que el IDENTITY se lea correctamente
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Estudiantes_Crear]
                    @AulaId INT,
                    @TipoDocumento VARCHAR(10),
                    @NumeroDocumento VARCHAR(15),
                    @Nombres VARCHAR(100),
                    @ApellidoPaterno VARCHAR(100),
                    @ApellidoMaterno VARCHAR(100),
                    @UsuarioIdApoderadoSasi VARCHAR(100) = NULL,
                    @NombreApoderado VARCHAR(150) = NULL,
                    @TelefonoApoderado VARCHAR(20) = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    INSERT INTO Estudiantes (
                        AulaId,
                        TipoDocumento,
                        NumeroDocumento,
                        Nombres,
                        ApellidoPaterno,
                        ApellidoMaterno,
                        UsuarioIdApoderadoSasi,
                        NombreApoderado,
                        TelefonoApoderado,
                        Estado,
                        FechaRegistro
                    )
                    VALUES (
                        @AulaId,
                        @TipoDocumento,
                        @NumeroDocumento,
                        UPPER(@Nombres),
                        UPPER(@ApellidoPaterno),
                        UPPER(@ApellidoMaterno),
                        @UsuarioIdApoderadoSasi,
                        @NombreApoderado,
                        @TelefonoApoderado,
                        1,
                        DATEADD(HOUR, -5, GETUTCDATE())
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ", suppressTransaction: true);

            // 2. sp_Gastos_Registrar: devolver el Id entero con CAST(SCOPE_IDENTITY() AS INT)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Gastos_Registrar]
                    @AulaId INT,
                    @Concepto VARCHAR(150),
                    @Categoria VARCHAR(50),
                    @Monto DECIMAL(10,2),
                    @FechaGasto DATE,
                    @TipoComprobante VARCHAR(30),
                    @NumeroComprobante VARCHAR(50) = NULL,
                    @Proveedor NVARCHAR(150) = NULL,
                    @Observacion NVARCHAR(300) = NULL,
                    @UrlComprobante VARCHAR(500) = NULL,
                    @UsuarioRegistro NVARCHAR(150)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    INSERT INTO GastosComite (
                        AulaId, Concepto, Categoria, Monto, FechaGasto,
                        TipoComprobante, NumeroComprobante, Proveedor, Observacion,
                        UrlComprobante, UsuarioRegistro
                    )
                    VALUES (
                        @AulaId, @Concepto, @Categoria, @Monto, @FechaGasto,
                        @TipoComprobante, @NumeroComprobante, @Proveedor, @Observacion,
                        @UrlComprobante, @UsuarioRegistro
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ", suppressTransaction: true);

            // 3. sp_Periodos_Crear: SET NOCOUNT ON (mantiene el retorno de SCOPE_IDENTITY)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Periodos_Crear
                    @Anio INT,
                    @FechaInicio DATETIME2,
                    @FechaFin DATETIME2,
                    @EsActivo BIT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @Nombre VARCHAR(100) = 'Año Lectivo ' + CAST(@Anio AS VARCHAR(4));

                    -- Si el nuevo periodo se marca como activo, desactivar los demás
                    IF @EsActivo = 1
                    BEGIN
                        UPDATE PeriodosLectivos SET EsActivo = 0;
                    END

                    INSERT INTO PeriodosLectivos (Anio, Nombre, EsActivo, FechaInicio, FechaFin)
                    VALUES (@Anio, @Nombre, @EsActivo, @FechaInicio, @FechaFin);

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ", suppressTransaction: true);

            // 4. sp_Comite_AsignarIntegrante: SET NOCOUNT ON (mantiene el retorno de SCOPE_IDENTITY)
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Comite_AsignarIntegrante]
                    @AulaId INT,
                    @UsuarioIdSasi VARCHAR(100),
                    @NombreCompleto VARCHAR(150),
                    @Email VARCHAR(100),
                    @Cargo VARCHAR(30)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Desactivar asignación previa para el mismo cargo en esta aula
                    UPDATE ComiteIntegrantes
                    SET Estado = 0
                    WHERE AulaId = @AulaId AND Cargo = @Cargo AND Estado = 1;

                    -- Insertar la nueva asignación (hora ajustada a Perú, UTC-5)
                    INSERT INTO ComiteIntegrantes (
                        AulaId,
                        UsuarioIdSasi,
                        NombreCompleto,
                        Email,
                        Cargo,
                        Estado,
                        FechaAsignacion
                    )
                    VALUES (
                        @AulaId,
                        @UsuarioIdSasi,
                        @NombreCompleto,
                        @Email,
                        @Cargo,
                        1,
                        DATEADD(HOUR, -5, GETUTCDATE())
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ", suppressTransaction: true);

            // 5. sp_Sistema_ResetBaseDeDatos: SOLO purga. El respaldo pre-purga lo
            //    genera la API (script SQL lógico) porque el BACKUP DATABASE físico
            //    no está disponible en el hosting (la BD corre en otro servidor).
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Sistema_ResetBaseDeDatos]
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- 1. EJECUTAR PURGA DE TABLAS REALES (EXCEPTUANDO InstitucionEducativa)
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

                        -- Reiniciar contadores AUTO-INCREMENTALES (IDENTITY) a 0: con las tablas
                        -- vacías, el siguiente valor asignado por SQL Server será efectivamente 1.
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
                        SELECT 1 AS Exitoso;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
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
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Estudiantes_Crear]
                    @AulaId INT,
                    @TipoDocumento VARCHAR(10),
                    @NumeroDocumento VARCHAR(15),
                    @Nombres VARCHAR(100),
                    @ApellidoPaterno VARCHAR(100),
                    @ApellidoMaterno VARCHAR(100),
                    @UsuarioIdApoderadoSasi VARCHAR(100) = NULL,
                    @NombreApoderado VARCHAR(150) = NULL,
                    @TelefonoApoderado VARCHAR(20) = NULL
                AS
                BEGIN
                    SET NOCOUNT OFF;
                    INSERT INTO Estudiantes (
                        AulaId, TipoDocumento, NumeroDocumento, Nombres, ApellidoPaterno,
                        ApellidoMaterno, UsuarioIdApoderadoSasi, NombreApoderado,
                        TelefonoApoderado, Estado, FechaRegistro
                    )
                    VALUES (
                        @AulaId, @TipoDocumento, @NumeroDocumento, UPPER(@Nombres), UPPER(@ApellidoPaterno),
                        UPPER(@ApellidoMaterno), @UsuarioIdApoderadoSasi, @NombreApoderado,
                        @TelefonoApoderado, 1, DATEADD(HOUR, -5, GETUTCDATE())
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ", suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Gastos_Registrar]
                    @AulaId INT,
                    @Concepto VARCHAR(150),
                    @Categoria VARCHAR(50),
                    @Monto DECIMAL(10,2),
                    @FechaGasto DATE,
                    @TipoComprobante VARCHAR(30),
                    @NumeroComprobante VARCHAR(50) = NULL,
                    @Proveedor NVARCHAR(150) = NULL,
                    @Observacion NVARCHAR(300) = NULL,
                    @UrlComprobante VARCHAR(500) = NULL,
                    @UsuarioRegistro NVARCHAR(150)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    INSERT INTO GastosComite (
                        AulaId, Concepto, Categoria, Monto, FechaGasto,
                        TipoComprobante, NumeroComprobante, Proveedor, Observacion,
                        UrlComprobante, UsuarioRegistro
                    )
                    VALUES (
                        @AulaId, @Concepto, @Categoria, @Monto, @FechaGasto,
                        @TipoComprobante, @NumeroComprobante, @Proveedor, @Observacion,
                        @UrlComprobante, @UsuarioRegistro
                    );
                    SELECT SCOPE_IDENTITY();
                END
            ", suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_Periodos_Crear
                    @Anio INT,
                    @FechaInicio DATETIME2,
                    @FechaFin DATETIME2,
                    @EsActivo BIT
                AS
                BEGIN
                    SET NOCOUNT OFF;
                    DECLARE @Nombre VARCHAR(100) = 'Año Lectivo ' + CAST(@Anio AS VARCHAR(4));
                    IF @EsActivo = 1
                    BEGIN
                        UPDATE PeriodosLectivos SET EsActivo = 0;
                    END
                    INSERT INTO PeriodosLectivos (Anio, Nombre, EsActivo, FechaInicio, FechaFin)
                    VALUES (@Anio, @Nombre, @EsActivo, @FechaInicio, @FechaFin);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ", suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_Comite_AsignarIntegrante]
                    @AulaId INT,
                    @UsuarioIdSasi VARCHAR(100),
                    @NombreCompleto VARCHAR(150),
                    @Email VARCHAR(100),
                    @Cargo VARCHAR(30)
                AS
                BEGIN
                    SET NOCOUNT OFF;
                    UPDATE ComiteIntegrantes
                    SET Estado = 0
                    WHERE AulaId = @AulaId AND Cargo = @Cargo AND Estado = 1;
                    INSERT INTO ComiteIntegrantes (
                        AulaId, UsuarioIdSasi, NombreCompleto, Email,
                        Cargo, Estado, FechaAsignacion
                    )
                    VALUES (
                        @AulaId, @UsuarioIdSasi, @NombreCompleto, @Email,
                        @Cargo, 1, DATEADD(HOUR, -5, GETUTCDATE())
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
            ", suppressTransaction: true);
        }
    }
}
