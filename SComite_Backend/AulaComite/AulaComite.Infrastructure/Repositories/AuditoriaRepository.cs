using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Application.Auditoria.Dtos;
using AulaComite.Application.Common.Interfaces;
using Dapper;

namespace AulaComite.Infrastructure.Repositories
{
    public class AuditoriaRepository : IAuditoriaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AuditoriaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<ResumenCajaAulaDto>> ObtenerResumenGeneralCajasAsync(int anioLectivo, string? nivel)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<ResumenCajaAulaDto>(
                "sp_Auditoria_ResumenGeneralCajas",
                new { AnioLectivo = anioLectivo, Nivel = nivel },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
