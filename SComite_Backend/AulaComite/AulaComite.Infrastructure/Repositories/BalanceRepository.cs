using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using Dapper;
using AulaComite.Application.Balance.Dtos;

namespace AulaComite.Infrastructure.Repositories
{
    public class BalanceRepository : IBalanceRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BalanceRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<BalanceConsolidado> ObtenerConsolidadoAsync(int aulaId, int anioLectivo, int? mes)
        {
            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<BalanceConsolidado>(
                "sp_Balance_ObtenerConsolidado",
                new { AulaId = aulaId, AnioLectivo = anioLectivo, Mes = mes },
                commandType: CommandType.StoredProcedure
            );

            return result ?? new BalanceConsolidado();
        }

        public async Task<IEnumerable<GastoCategoriaResumen>> ObtenerGastosPorCategoriaAsync(int aulaId, int anioLectivo, int? mes)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<GastoCategoriaResumen>(
                "sp_Balance_ObtenerGastosPorCategoria",
                new { AulaId = aulaId, AnioLectivo = anioLectivo, Mes = mes },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<GastoComiteDTO>> ObtenerGastosDetalleAsync(int aulaId, int anioLectivo, int? mes)
        {
            using var connection = _connectionFactory.CreateConnection();

            // Consulta directa para traer el desglose completo de gastos
            var sql = @"
                SELECT 
                    Id,
                    FechaGasto,
                    Concepto,
                    Categoria,
                    Monto,
                    TipoComprobante,
                    NumeroComprobante,
                    Proveedor
                FROM GastosComite
                WHERE AulaId = @AulaId
                  AND (@Mes IS NULL OR @Mes = 0 OR (MONTH(FechaGasto) = @Mes AND YEAR(FechaGasto) = @AnioLectivo))
                ORDER BY FechaGasto DESC;";

            return await connection.QueryAsync<GastoComiteDTO>(sql, new { AulaId = aulaId, AnioLectivo = anioLectivo, Mes = mes });
        }
    }
}
