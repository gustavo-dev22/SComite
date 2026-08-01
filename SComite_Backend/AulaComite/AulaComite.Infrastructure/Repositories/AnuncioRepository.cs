using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Domain.Entities;
using AulaComite.Application.Common.Interfaces;
using Dapper;

namespace AulaComite.Infrastructure.Repositories
{
    public class AnuncioRepository : IAnuncioRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AnuncioRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<AnuncioComite>> ObtenerPorAulaAsync(int aulaId, int anioLectivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<AnuncioComite>(
                "sp_Anuncios_ListarPorAula",
                new { AulaId = aulaId, AnioLectivo = anioLectivo },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> GuardarAsync(int id, int aulaId, string titulo, string contenido, string categoria, bool esFijado, string? urlAdjunto, string usuarioRegistro)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(
                "sp_Anuncios_Guardar",
                new { Id = id, AulaId = aulaId, Titulo = titulo, Contenido = contenido, Categoria = categoria, EsFijado = esFijado, UrlAdjunto = urlAdjunto, UsuarioRegistro = usuarioRegistro },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> EliminarAsync(int id, int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "sp_Anuncios_Eliminar",
                new { Id = id, AulaId = aulaId },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }
    }
}
