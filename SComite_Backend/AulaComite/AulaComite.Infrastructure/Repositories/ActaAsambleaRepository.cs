using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Domain.Entities;
using AulaComite.Application.Common.Interfaces;
using Dapper;

namespace AulaComite.Infrastructure.Repositories
{
    public class ActaAsambleaRepository : IActaAsambleaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ActaAsambleaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<ActaAsambleaComite>> ObtenerPorAulaAsync(int aulaId, int anioLectivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<ActaAsambleaComite>(
                "sp_ActasAsamblea_ListarPorAula",
                new { AulaId = aulaId, AnioLectivo = anioLectivo },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> GuardarAsync(int id, int aulaId, string numeroActa, string titulo, DateTime fechaReunion, string agendaAcuerdos, string estadoActa, string? urlDocumentoPdf, string usuarioRegistro)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(
                "sp_ActasAsamblea_Guardar",
                new { Id = id, AulaId = aulaId, NumeroActa = numeroActa, Titulo = titulo, FechaReunion = fechaReunion, AgendaAcuerdos = agendaAcuerdos, EstadoActa = estadoActa, UrlDocumentoPdf = urlDocumentoPdf, UsuarioRegistro = usuarioRegistro },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> EliminarAsync(int id, int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteScalarAsync<int>(
                "sp_ActasAsamblea_Eliminar",
                new { Id = id, AulaId = aulaId },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }
        public async Task<string> ObtenerSiguienteNumeroActaAsync(int aulaId, int anioLectivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            var siguienteNumero = await connection.QueryFirstOrDefaultAsync<string>(
                "sp_ActasAsamblea_ObtenerSiguienteNumero",
                new { AulaId = aulaId, AnioLectivo = anioLectivo },
                commandType: CommandType.StoredProcedure
            );

            return siguienteNumero ?? $"ACTA-001-{anioLectivo}";
        }

    }
}
