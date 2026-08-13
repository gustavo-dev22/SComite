using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Application.Common.Interfaces;
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
            using var connection = _connectionFactory.CreateConnection();

            // Ruta dinámica donde SQL Server guardará el respaldo pre-purga
            // (dentro de la carpeta del servidor para garantizar permisos de escritura).
            var rutaFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            Directory.CreateDirectory(rutaFolder);

            var result = await connection.ExecuteScalarAsync<int>(
                "sp_Sistema_ResetBaseDeDatos",
                new { RutaBackupFolder = rutaFolder },
                commandType: CommandType.StoredProcedure
            );
            return result == 1;
        }

        public async Task<byte[]> GenerarBackupScriptSqlAsync()
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = new StringBuilder();
            var fechaHora = DateTimeHelper.ObtenerHoraPeru().ToString("dd/MM/yyyy HH:mm:ss");

            sql.AppendLine("-- ===========================================================");
            sql.AppendLine($"-- BACKUP MANUAL COMPLETO - SISTEMA DE COMITÉ DE AULA");
            sql.AppendLine($"-- FECHA DE EMISIÓN: {fechaHora}");
            sql.AppendLine("-- ===========================================================");
            sql.AppendLine("USE [db_ComiteAula];");
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
                sql.AppendLine($"INSERT INTO PeriodosLectivos (Anio, FechaInicio, FechaFin, Estado) VALUES ({p.Anio}, '{p.FechaInicio:yyyy-MM-dd}', '{p.FechaFin:yyyy-MM-dd}', {p.Estado});");
            }

            // 3. Aulas
            var aulas = await connection.QueryAsync("SELECT * FROM Aulas");
            sql.AppendLine("\n-- TABLA: Aulas");
            foreach (var a in aulas)
            {
                sql.AppendLine($"INSERT INTO Aulas (PeriodoId, Grado, Seccion, Nivel, Estado) VALUES ({a.PeriodoId}, {a.Grado}, '{EscaparSql(a.Seccion)}', '{EscaparSql(a.Nivel)}', {a.Estado});");
            }

            // 4. Estudiantes
            var estudiantes = await connection.QueryAsync("SELECT * FROM Estudiantes");
            sql.AppendLine("\n-- TABLA: Estudiantes");
            foreach (var e in estudiantes)
            {
                var apoderadoSasi = string.IsNullOrEmpty(e.UsuarioIdApoderadoSasi) ? "NULL" : $"'{EscaparSql(e.UsuarioIdApoderadoSasi)}'";
                var nomApoderado = string.IsNullOrEmpty(e.NombreApoderado) ? "NULL" : $"'{EscaparSql(e.NombreApoderado)}'";
                var telApoderado = string.IsNullOrEmpty(e.TelefonoApoderado) ? "NULL" : $"'{EscaparSql(e.TelefonoApoderado)}'";

                sql.AppendLine($"INSERT INTO Estudiantes (AulaId, TipoDocumento, NumeroDocumento, Nombres, ApellidoPaterno, ApellidoMaterno, UsuarioIdApoderadoSasi, NombreApoderado, TelefonoApoderado, Estado) VALUES ({e.AulaId}, '{EscaparSql(e.TipoDocumento)}', '{EscaparSql(e.NumeroDocumento)}', '{EscaparSql(e.Nombres)}', '{EscaparSql(e.ApellidoPaterno)}', '{EscaparSql(e.ApellidoMaterno)}', {apoderadoSasi}, {nomApoderado}, {telApoderado}, {e.Estado});");
            }

            // 5. ComiteIntegrantes
            var comite = await connection.QueryAsync("SELECT * FROM ComiteIntegrantes");
            sql.AppendLine("\n-- TABLA: ComiteIntegrantes");
            foreach (var c in comite)
            {
                sql.AppendLine($"INSERT INTO ComiteIntegrantes (AulaId, Cargo, UsuarioIdSasi, NombreCompleto, Telefono, FechaAsignacion, Estado) VALUES ({c.AulaId}, '{EscaparSql(c.Cargo)}', '{EscaparSql(c.UsuarioIdSasi)}', '{EscaparSql(c.NombreCompleto)}', '{EscaparSql(c.Telefono)}', '{c.FechaAsignacion:yyyy-MM-dd HH:mm:ss}', {c.Estado});");
            }

            // 6. ActividadesComite
            var actividades = await connection.QueryAsync("SELECT * FROM ActividadesComite");
            sql.AppendLine("\n-- TABLA: ActividadesComite");
            foreach (var act in actividades)
            {
                var desc = string.IsNullOrEmpty(act.Descripcion) ? "NULL" : $"'{EscaparSql(act.Descripcion)}'";
                sql.AppendLine($"INSERT INTO ActividadesComite (AulaId, NombreActividad, Descripcion, FechaProgramada, MontoPresupuestado, CuotaSugeridaPorAlumno, Estado) VALUES ({act.AulaId}, '{EscaparSql(act.NombreActividad)}', {desc}, '{act.FechaProgramada:yyyy-MM-dd}', {act.MontoPresupuestado}, {act.CuotaSugeridaPorAlumno}, '{EscaparSql(act.Estado)}');");
            }

            // 7. Cuotas
            var cuotas = await connection.QueryAsync("SELECT * FROM Cuotas");
            sql.AppendLine("\n-- TABLA: Cuotas");
            foreach (var cu in cuotas)
            {
                sql.AppendLine($"INSERT INTO Cuotas (AulaId, Concepto, MontoMembresia, FechaVencimiento, Estado) VALUES ({cu.AulaId}, '{EscaparSql(cu.Concepto)}', {cu.MontoMembresia}, '{cu.FechaVencimiento:yyyy-MM-dd}', '{EscaparSql(cu.Estado)}');");
            }

            // 8. CuotaDetalleEstudiante
            var cuotaDetalles = await connection.QueryAsync("SELECT * FROM CuotaDetalleEstudiante");
            sql.AppendLine("\n-- TABLA: CuotaDetalleEstudiante");
            foreach (var cd in cuotaDetalles)
            {
                var fPago = cd.FechaPago == null ? "NULL" : $"'{cd.FechaPago:yyyy-MM-dd HH:mm:ss}'";
                var nComprobante = string.IsNullOrEmpty(cd.NumeroComprobante) ? "NULL" : $"'{EscaparSql(cd.NumeroComprobante)}'";
                var obs = string.IsNullOrEmpty(cd.Observaciones) ? "NULL" : $"'{EscaparSql(cd.Observaciones)}'";

                sql.AppendLine($"INSERT INTO CuotaDetalleEstudiante (CuotaId, EstudianteId, MontoMembresia, EstadoPago, FechaPago, NumeroComprobante, Observaciones) VALUES ({cd.CuotaId}, {cd.EstudianteId}, {cd.MontoMembresia}, '{EscaparSql(cd.EstadoPago)}', {fPago}, {nComprobante}, {obs});");
            }

            // 9. GastosComite
            var gastos = await connection.QueryAsync("SELECT * FROM GastosComite");
            sql.AppendLine("\n-- TABLA: GastosComite");
            foreach (var g in gastos)
            {
                var numDoc = string.IsNullOrEmpty(g.NumeroDocumento) ? "NULL" : $"'{EscaparSql(g.NumeroDocumento)}'";
                var urlAdj = string.IsNullOrEmpty(g.UrlComprobante) ? "NULL" : $"'{EscaparSql(g.UrlComprobante)}'";

                sql.AppendLine($"INSERT INTO GastosComite (AulaId, Concepto, Categoria, MontoGasto, FechaGasto, TipoComprobante, NumeroDocumento, UrlComprobante, UsuarioRegistro) VALUES ({g.AulaId}, '{EscaparSql(g.Concepto)}', '{EscaparSql(g.Categoria)}', {g.MontoGasto}, '{g.FechaGasto:yyyy-MM-dd}', '{EscaparSql(g.TipoComprobante)}', {numDoc}, {urlAdj}, '{EscaparSql(g.UsuarioRegistro)}');");
            }

            // 10. DonacionesComite
            var donaciones = await connection.QueryAsync("SELECT * FROM DonacionesComite");
            sql.AppendLine("\n-- TABLA: DonacionesComite");
            foreach (var d in donaciones)
            {
                var obs = string.IsNullOrEmpty(d.Observaciones) ? "NULL" : $"'{EscaparSql(d.Observaciones)}'";
                sql.AppendLine($"INSERT INTO DonacionesComite (AulaId, NombreDonante, Concepto, Monto, FechaDonacion, Observaciones) VALUES ({d.AulaId}, '{EscaparSql(d.NombreDonante)}', '{EscaparSql(d.Concepto)}', {d.Monto}, '{d.FechaDonacion:yyyy-MM-dd}', {obs});");
            }

            // 11. ActasAsambleaComite
            var actas = await connection.QueryAsync("SELECT * FROM ActasAsambleaComite");
            sql.AppendLine("\n-- TABLA: ActasAsambleaComite");
            foreach (var ac in actas)
            {
                var urlPdf = string.IsNullOrEmpty(ac.UrlDocumentoPdf) ? "NULL" : $"'{EscaparSql(ac.UrlDocumentoPdf)}'";
                sql.AppendLine($"INSERT INTO ActasAsambleaComite (AulaId, NumeroActa, Titulo, FechaReunion, AgendaAcuerdos, EstadoActa, UrlDocumentoPdf, UsuarioRegistro) VALUES ({ac.AulaId}, '{EscaparSql(ac.NumeroActa)}', '{EscaparSql(ac.Titulo)}', '{ac.FechaReunion:yyyy-MM-dd}', '{EscaparSql(ac.AgendaAcuerdos)}', '{EscaparSql(ac.EstadoActa)}', {urlPdf}, '{EscaparSql(ac.UsuarioRegistro)}');");
            }

            // 12. AnunciosComite
            var anuncios = await connection.QueryAsync("SELECT * FROM AnunciosComite");
            sql.AppendLine("\n-- TABLA: AnunciosComite");
            foreach (var an in anuncios)
            {
                var urlAdj = string.IsNullOrEmpty(an.UrlAdjunto) ? "NULL" : $"'{EscaparSql(an.UrlAdjunto)}'";
                sql.AppendLine($"INSERT INTO AnunciosComite (AulaId, Titulo, Contenido, Categoria, EsFijado, UrlAdjunto, CantidadVistas, UsuarioRegistro, FechaPublicacion) VALUES ({an.AulaId}, '{EscaparSql(an.Titulo)}', '{EscaparSql(an.Contenido)}', '{EscaparSql(an.Categoria)}', {(an.EsFijado ? 1 : 0)}, {urlAdj}, {an.CantidadVistas ?? 0}, '{EscaparSql(an.UsuarioRegistro)}', '{an.FechaPublicacion:yyyy-MM-dd HH:mm:ss}');");
            }

            // 13. AnuncioLecturasEstudiante
            var lecturasAnuncios = await connection.QueryAsync("SELECT * FROM AnuncioLecturasEstudiante");
            sql.AppendLine("\n-- TABLA: AnuncioLecturasEstudiante");
            foreach (var al in lecturasAnuncios)
            {
                sql.AppendLine($"INSERT INTO AnuncioLecturasEstudiante (AnuncioId, EstudianteId, UsuarioApoderado, FechaLectura) VALUES ({al.AnuncioId}, {al.EstudianteId}, '{EscaparSql(al.UsuarioApoderado)}', '{al.FechaLectura:yyyy-MM-dd HH:mm:ss}');");
            }

            // 14. LogsSistema
            var logs = await connection.QueryAsync("SELECT * FROM LogsSistema");
            sql.AppendLine("\n-- TABLA: LogsSistema");
            foreach (var l in logs)
            {
                var usr = string.IsNullOrEmpty(l.Usuario) ? "NULL" : $"'{EscaparSql(l.Usuario)}'";
                sql.AppendLine($"INSERT INTO LogsSistema (Nivel, Modulo, Accion, Detalle, Usuario, FechaHora) VALUES ('{EscaparSql(l.Nivel)}', '{EscaparSql(l.Modulo)}', '{EscaparSql(l.Accion)}', '{EscaparSql(l.Detalle)}', {usr}, '{l.FechaHora:yyyy-MM-dd HH:mm:ss}');");
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
