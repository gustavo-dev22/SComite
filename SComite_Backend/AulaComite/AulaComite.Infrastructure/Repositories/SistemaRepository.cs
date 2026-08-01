using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Application.Common.Interfaces;
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
            var result = await connection.ExecuteScalarAsync<int>(
                "sp_Sistema_ResetBaseDeDatos",
                commandType: CommandType.StoredProcedure
            );
            return result == 1;
        }
    }
}
