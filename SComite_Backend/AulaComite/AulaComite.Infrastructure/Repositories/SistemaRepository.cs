using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Data;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Domain.Common;
using Dapper;

namespace AulaComite.Infrastructure.Repositories
{
    public class SistemaRepository : ISistemaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SistemaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> ResetBaseDeDatosAsync()
        {
            // Ruta donde la API guarda el respaldo pre-purga (script SQL lógico).
            // En el hosting la BD corre en un servidor separado y NO puede ejecutar
            // BACKUP DATABASE hacia esta carpeta, por lo que el respaldo se genera
            // aquí (script INSERT) antes de invocar la purga.
            var rutaFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            Directory.CreateDirectory(rutaFolder);

            var backupBytes = await GenerarBackupScriptSqlAsync();
            var archivoBackup = Path.Combine(rutaFolder, $"Backup_PrePurga_{DateTimeHelper.ObtenerHoraPeru():yyyyMMdd_HHmmss}.sql");
            await File.WriteAllBytesAsync(archivoBackup, backupBytes);

            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.ExecuteScalarAsync<int>(
                "sp_Sistema_ResetBaseDeDatos",
                commandType: CommandType.StoredProcedure
            );
            return result == 1;
        }

        public async Task<byte[]> GenerarBackupScriptSqlAsync()
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = new StringBuilder();
            var fechaHora = DateTimeHelper.ObtenerHoraPeru().ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            sql.AppendLine("-- ===========================================================");
            sql.AppendLine($"-- BACKUP MANUAL COMPLETO - SISTEMA DE COMITÉ DE AULA");
            sql.AppendLine($"-- FECHA DE EMISIÓN: {fechaHora}");
            sql.AppendLine("-- ===========================================================");
            // 🛡️ T2.4: Nombre de BD real tomado de la conexión activa (antes estaba hardcodeado).
            sql.AppendLine($"USE [{connection.Database}];");
            sql.AppendLine("GO\n");
            sql.AppendLine("SET NOCOUNT ON;");
            // M18: Deshabilitar FK de forma PORTABLE (sin sp_MSforeachtable, no disponible en todas las ediciones).
            sql.AppendLine("DECLARE @CmdDeshabilitar NVARCHAR(MAX) = N'';");
            sql.AppendLine("SELECT @CmdDeshabilitar = @CmdDeshabilitar + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(t.object_id)) + N'.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;' + CHAR(13) + CHAR(10)");
            sql.AppendLine("FROM sys.tables t WHERE t.is_ms_shipped = 0;");
            sql.AppendLine("EXEC sp_executesql @CmdDeshabilitar;");
            sql.AppendLine("GO\n");

            // 2. PeriodosLectivos
            var periodos = await connection.QueryAsync("SELECT * FROM PeriodosLectivos");
            sql.AppendLine("\n-- TABLA: PeriodosLectivos");
            foreach (var p in periodos)
            {
                var nombre = string.IsNullOrEmpty(p.Nombre) ? "NULL" : $"'{EscaparSql(p.Nombre)}'";
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO PeriodosLectivos (Anio, Nombre, FechaInicio, FechaFin, EsActivo) VALUES ({p.Anio}, {nombre}, '{p.FechaInicio:yyyy-MM-dd}', '{p.FechaFin:yyyy-MM-dd}', {p.EsActivo});"));
            }

            // 3. Aulas
            var aulas = await connection.QueryAsync("SELECT * FROM Aulas");
            sql.AppendLine("\n-- TABLA: Aulas");
            foreach (var a in aulas)
            {
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO Aulas (PeriodoId, Grado, Seccion, Nivel, NombreDisplay, Estado) VALUES ({a.PeriodoId}, {a.Grado}, '{EscaparSql(a.Seccion)}', '{EscaparSql(a.Nivel)}', '{EscaparSql(a.NombreDisplay)}', {a.Estado});"));
            }

            // 4. Estudiantes
            var estudiantes = await connection.QueryAsync("SELECT * FROM Estudiantes");
            sql.AppendLine("\n-- TABLA: Estudiantes");
            foreach (var e in estudiantes)
            {
                var apoderadoSasi = string.IsNullOrEmpty(e.UsuarioIdApoderadoSasi) ? "NULL" : $"'{EscaparSql(e.UsuarioIdApoderadoSasi)}'";
                var nomApoderado = string.IsNullOrEmpty(e.NombreApoderado) ? "NULL" : $"'{EscaparSql(e.NombreApoderado)}'";
                // 🛡️ T2.4: Se enmascaran datos personales sensibles (DNI y teléfono del apoderado)
                // en el volcado de texto para no exponer PII en el archivo de respaldo.
                var dni = PiiMasker.EnmascararDocumento(e.NumeroDocumento);
                var tel = PiiMasker.EnmascararTelefono(e.TelefonoApoderado);

                sql.AppendLine(FormattableString.Invariant($"INSERT INTO Estudiantes (AulaId, TipoDocumento, NumeroDocumento, Nombres, ApellidoPaterno, ApellidoMaterno, UsuarioIdApoderadoSasi, NombreApoderado, TelefonoApoderado, Estado) VALUES ({e.AulaId}, '{EscaparSql(e.TipoDocumento)}', '{EscaparSql(dni)}', '{EscaparSql(e.Nombres)}', '{EscaparSql(e.ApellidoPaterno)}', '{EscaparSql(e.ApellidoMaterno)}', {apoderadoSasi}, {nomApoderado}, '{EscaparSql(tel)}', {e.Estado});"));
            }

            // 5. ComiteIntegrantes
            var comite = await connection.QueryAsync("SELECT * FROM ComiteIntegrantes");
            sql.AppendLine("\n-- TABLA: ComiteIntegrantes");
            foreach (var c in comite)
            {
                var email = string.IsNullOrEmpty(c.Email) ? "NULL" : $"'{EscaparSql(c.Email)}'";
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO ComiteIntegrantes (AulaId, Cargo, UsuarioIdSasi, NombreCompleto, Email, FechaAsignacion, Estado) VALUES ({c.AulaId}, '{EscaparSql(c.Cargo)}', '{EscaparSql(c.UsuarioIdSasi)}', '{EscaparSql(c.NombreCompleto)}', {email}, '{c.FechaAsignacion:yyyy-MM-dd HH:mm:ss}', {c.Estado});"));
            }

            // 6. ActividadesComite
            var actividades = await connection.QueryAsync("SELECT * FROM ActividadesComite");
            sql.AppendLine("\n-- TABLA: ActividadesComite");
            foreach (var act in actividades)
            {
                var desc = string.IsNullOrEmpty(act.Descripcion) ? "NULL" : $"'{EscaparSql(act.Descripcion)}'";
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO ActividadesComite (AulaId, NombreActividad, Descripcion, FechaProgramada, MontoPresupuestado, CuotaSugeridaPorAlumno, Estado) VALUES ({act.AulaId}, '{EscaparSql(act.NombreActividad)}', {desc}, '{act.FechaProgramada:yyyy-MM-dd}', {act.MontoPresupuestado ?? 0}, {act.CuotaSugeridaPorAlumno ?? 0}, '{EscaparSql(act.Estado)}');"));
            }

            // 7. Cuotas
            var cuotas = await connection.QueryAsync("SELECT * FROM Cuotas");
            sql.AppendLine("\n-- TABLA: Cuotas");
            foreach (var cu in cuotas)
            {
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO Cuotas (AulaId, Concepto, MontoIndividual, FechaVencimiento, Estado) VALUES ({cu.AulaId}, '{EscaparSql(cu.Concepto)}', {cu.MontoIndividual}, '{cu.FechaVencimiento:yyyy-MM-dd}', '{EscaparSql(cu.Estado)}');"));
            }

            // 8. CuotaDetalleEstudiante
            var cuotaDetalles = await connection.QueryAsync("SELECT * FROM CuotaDetalleEstudiante");
            sql.AppendLine("\n-- TABLA: CuotaDetalleEstudiante");
            foreach (var cd in cuotaDetalles)
            {
                var fUltimoPago = cd.FechaUltimoPago == null ? "NULL" : $"'{cd.FechaUltimoPago:yyyy-MM-dd HH:mm:ss}'";
                var motivo = string.IsNullOrEmpty(cd.MotivoExoneracion) ? "NULL" : $"'{EscaparSql(cd.MotivoExoneracion)}'";
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO CuotaDetalleEstudiante (CuotaId, EstudianteId, MontoAsignado, MontoPagado, EstadoPago, FechaUltimoPago, MotivoExoneracion) VALUES ({cd.CuotaId}, {cd.EstudianteId}, {cd.MontoAsignado}, {cd.MontoPagado ?? 0}, '{EscaparSql(cd.EstadoPago)}', {fUltimoPago}, {motivo});"));
            }

            // 9. GastosComite
            var gastos = await connection.QueryAsync("SELECT * FROM GastosComite");
            sql.AppendLine("\n-- TABLA: GastosComite");
            foreach (var g in gastos)
            {
                var nComprobante = string.IsNullOrEmpty(g.NumeroComprobante) ? "NULL" : $"'{EscaparSql(g.NumeroComprobante)}'";
                var urlAdj = string.IsNullOrEmpty(g.UrlComprobante) ? "NULL" : $"'{EscaparSql(g.UrlComprobante)}'";
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO GastosComite (AulaId, Concepto, Categoria, Monto, FechaGasto, TipoComprobante, NumeroComprobante, UrlComprobante, UsuarioRegistro) VALUES ({g.AulaId}, '{EscaparSql(g.Concepto)}', '{EscaparSql(g.Categoria)}', {g.Monto}, '{g.FechaGasto:yyyy-MM-dd}', '{EscaparSql(g.TipoComprobante)}', {nComprobante}, {urlAdj}, '{EscaparSql(g.UsuarioRegistro)}');"));
            }

            // 10. DonacionesComite
            var donaciones = await connection.QueryAsync("SELECT * FROM DonacionesComite");
            sql.AppendLine("\n-- TABLA: DonacionesComite");
            foreach (var d in donaciones)
            {
                var obs = string.IsNullOrEmpty(d.Observacion) ? "NULL" : $"'{EscaparSql(d.Observacion)}'";
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO DonacionesComite (AulaId, Donante, Concepto, Monto, FechaDonacion, Observacion) VALUES ({d.AulaId}, '{EscaparSql(d.Donante)}', '{EscaparSql(d.Concepto)}', {d.Monto}, '{d.FechaDonacion:yyyy-MM-dd}', {obs});"));
            }

            // 11. ActasAsambleaComite
            var actas = await connection.QueryAsync("SELECT * FROM ActasAsambleaComite");
            sql.AppendLine("\n-- TABLA: ActasAsambleaComite");
            foreach (var ac in actas)
            {
                var urlPdf = string.IsNullOrEmpty(ac.UrlDocumentoPdf) ? "NULL" : $"'{EscaparSql(ac.UrlDocumentoPdf)}'";
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO ActasAsambleaComite (AulaId, NumeroActa, Titulo, FechaReunion, AgendaAcuerdos, EstadoActa, UrlDocumentoPdf, UsuarioRegistro) VALUES ({ac.AulaId}, '{EscaparSql(ac.NumeroActa)}', '{EscaparSql(ac.Titulo)}', '{ac.FechaReunion:yyyy-MM-dd}', '{EscaparSql(ac.AgendaAcuerdos)}', '{EscaparSql(ac.EstadoActa)}', {urlPdf}, '{EscaparSql(ac.UsuarioRegistro)}');"));
            }

            // 12. AnunciosComite
            var anuncios = await connection.QueryAsync("SELECT * FROM AnunciosComite");
            sql.AppendLine("\n-- TABLA: AnunciosComite");
            foreach (var an in anuncios)
            {
                var urlAdj = string.IsNullOrEmpty(an.UrlAdjunto) ? "NULL" : $"'{EscaparSql(an.UrlAdjunto)}'";
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO AnunciosComite (AulaId, Titulo, Contenido, Categoria, EsFijado, UrlAdjunto, CantidadVistas, UsuarioRegistro, FechaPublicacion) VALUES ({an.AulaId}, '{EscaparSql(an.Titulo)}', '{EscaparSql(an.Contenido)}', '{EscaparSql(an.Categoria)}', {(an.EsFijado ? 1 : 0)}, {urlAdj}, {an.CantidadVistas ?? 0}, '{EscaparSql(an.UsuarioRegistro)}', '{an.FechaPublicacion:yyyy-MM-dd HH:mm:ss}');"));
            }

            // 13. AnuncioLecturasEstudiante
            var lecturasAnuncios = await connection.QueryAsync("SELECT * FROM AnuncioLecturasEstudiante");
            sql.AppendLine("\n-- TABLA: AnuncioLecturasEstudiante");
            foreach (var al in lecturasAnuncios)
            {
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO AnuncioLecturasEstudiante (AnuncioId, EstudianteId, UsuarioApoderado, FechaLectura) VALUES ({al.AnuncioId}, {al.EstudianteId}, '{EscaparSql(al.UsuarioApoderado)}', '{al.FechaLectura:yyyy-MM-dd HH:mm:ss}');"));
            }

            // 14. LogsSistema
            var logs = await connection.QueryAsync("SELECT * FROM LogsSistema");
            sql.AppendLine("\n-- TABLA: LogsSistema");
            foreach (var l in logs)
            {
                var usr = string.IsNullOrEmpty(l.Usuario) ? "NULL" : $"'{EscaparSql(l.Usuario)}'";
                var ip = string.IsNullOrEmpty(l.IP) ? "NULL" : $"'{EscaparSql(l.IP)}'";
                var detalle = string.IsNullOrEmpty(l.DetalleException) ? "NULL" : $"'{EscaparSql(l.DetalleException)}'";
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO LogsSistema (Nivel, Modulo, Accion, Mensaje, Usuario, IP, DetalleException, Fecha) VALUES ('{EscaparSql(l.Nivel)}', '{EscaparSql(l.Modulo)}', '{EscaparSql(l.Accion)}', '{EscaparSql(l.Mensaje)}', {usr}, {ip}, {detalle}, '{l.Fecha:yyyy-MM-dd HH:mm:ss}');"));
            }

            sql.AppendLine("\n-- Re-habilitar FK de forma PORTABLE (sin sp_MSforeachtable):");
            sql.AppendLine("DECLARE @CmdHabilitar NVARCHAR(MAX) = N'';");
            sql.AppendLine("SELECT @CmdHabilitar = @CmdHabilitar + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(t.object_id)) + N'.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13) + CHAR(10)");
            sql.AppendLine("FROM sys.tables t WHERE t.is_ms_shipped = 0");
            sql.AppendLine("  AND EXISTS (SELECT 1 FROM sys.foreign_keys fk WHERE fk.parent_object_id = t.object_id OR fk.referenced_object_id = t.object_id);");
            sql.AppendLine("EXEC sp_executesql @CmdHabilitar;");
            sql.AppendLine("GO");

            return Encoding.UTF8.GetBytes(sql.ToString());
        }

        // Método auxiliar para evitar fallos por comillas simples en el texto SQL
        private static string EscaparSql(object? input)
        {
            if (input == null || input == DBNull.Value) return string.Empty;
            return input.ToString()!.Replace("'", "''");
        }
    }
}
