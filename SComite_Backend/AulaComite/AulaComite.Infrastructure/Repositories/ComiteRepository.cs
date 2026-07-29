using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using Dapper;
using AulaComite.Domain.Entities;
using System.Data;

namespace AulaComite.Infrastructure.Repositories
{
    public class ComiteRepository : IComiteRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ComiteRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<ComiteIntegrante>> ObtenerPorAulaAsync(int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<ComiteIntegrante>(
                "sp_Comite_ObtenerPorAula",
                new { AulaId = aulaId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> AsignarIntegranteAsync(ComiteIntegrante integrante)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(
                "sp_Comite_AsignarIntegrante",
                new
                {
                    AulaId = integrante.AulaId,
                    UsuarioIdSasi = integrante.UsuarioIdSasi,
                    NombreCompleto = integrante.NombreCompleto,
                    Email = integrante.Email,
                    Cargo = integrante.Cargo
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> EliminarIntegranteAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "sp_Comite_EliminarIntegrante",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }
    }
}
