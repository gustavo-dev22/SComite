using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using Dapper;
using System.Data;

namespace AulaComite.Infrastructure.Repositories
{
    public class PeriodoRepository : IPeriodoRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PeriodoRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CrearAsync(PeriodoLectivo p, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? _connectionFactory.CreateConnection();
            try
            {
                return await connection.ExecuteScalarAsync<int>(
                    "sp_Periodos_Crear",
                    new
                    {
                        Anio = p.Anio,
                        FechaInicio = p.FechaInicio,
                        FechaFin = p.FechaFin,
                        EsActivo = p.EsActivo
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure
                );
            }
            finally
            {
                if (transaction == null) connection.Dispose();
            }
        }

        public async Task<bool> ActualizarAsync(PeriodoLectivo p)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "sp_Periodos_Actualizar",
                new
                {
                    Id = p.Id,
                    Anio = p.Anio,
                    FechaInicio = p.FechaInicio,
                    FechaFin = p.FechaFin,
                    EsActivo = p.EsActivo
                },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }

        public async Task<bool> ExisteAnioAsync(int anio)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<bool>(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM PeriodosLectivos WHERE Anio = @Anio) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END",
                new { Anio = anio }
            );
        }

        public async Task<PeriodoLectivo?> ObtenerPorIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<PeriodoLectivo>(
                "SELECT Id, Anio, Nombre, EsActivo, FechaInicio, FechaFin FROM PeriodosLectivos WHERE Id = @Id",
                new { Id = id }
            );
        }

        public async Task<bool> CambiarEstadoAsync(int id, bool esActivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "sp_Periodos_CambiarEstado",
                new { Id = id, EsActivo = esActivo },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }
    }
}
