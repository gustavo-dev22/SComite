using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Donaciones.Dtos;
using Dapper;

namespace AulaComite.Infrastructure.Repositories
{
    public class DonacionRepository : IDonacionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DonacionRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<DonacionDTO>> ObtenerPorAulaAsync(int aulaId, int anioLectivo, int? mes)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<DonacionDTO>(
                "sp_Donaciones_ListarPorAula",
                new { AulaId = aulaId, AnioLectivo = anioLectivo, Mes = mes },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> GuardarAsync(int id, int aulaId, string donante, decimal monto, DateTime fechaDonacion, string concepto, string? observacion)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(
                "sp_Donaciones_Guardar",
                new { Id = id, AulaId = aulaId, Donante = donante, Monto = monto, FechaDonacion = fechaDonacion, Concepto = concepto, Observacion = observacion },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> EliminarAsync(int id, int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "sp_Donaciones_Eliminar",
                new { Id = id, AulaId = aulaId },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }
    }
}
