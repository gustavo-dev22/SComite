using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Balance.Dtos;
using AulaComite.Application.Gastos.Dtos;
using AulaComite.Domain.Entities;
using Dapper;

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

        public async Task<IEnumerable<GastoCategoriaResumenDto>> ObtenerGastosPorCategoriaAsync(int aulaId, int anioLectivo, int? mes)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<GastoCategoriaResumenDto>(
                "sp_Balance_ObtenerGastosPorCategoria",
                new { AulaId = aulaId, AnioLectivo = anioLectivo, Mes = mes },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<GastoComiteDto>> ObtenerGastosDetalleAsync(int aulaId, int anioLectivo, int? mes)
        {
            using var connection = _connectionFactory.CreateConnection();

            // 🚀 T3.2: Rango continuo de fechas (SARGable) en lugar de MONTH()/YEAR().
            // Permite que el optimizador use IX_Gastos_AulaId_Fecha (AulaId, FechaGasto).
            DateTime? fechaInicio = mes.HasValue && mes.Value > 0
                ? new DateTime(anioLectivo, mes.Value, 1)
                : (DateTime?)null;
            DateTime? fechaFin = fechaInicio.HasValue ? fechaInicio.Value.AddMonths(1) : (DateTime?)null;

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
                  AND (@FechaInicio IS NULL OR (FechaGasto >= @FechaInicio AND FechaGasto < @FechaFin))
                ORDER BY FechaGasto DESC;";

            return await connection.QueryAsync<GastoComiteDto>(
                sql,
                new { AulaId = aulaId, FechaInicio = fechaInicio, FechaFin = fechaFin });
        }
    }
}
