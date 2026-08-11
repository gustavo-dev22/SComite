using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Gastos.Dtos;
using AulaComite.Domain.Entities;
using Dapper;

namespace AulaComite.Infrastructure.Repositories
{
    public class GastoRepository : IGastoRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public GastoRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> RegistrarAsync(GastoComite gasto, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? _connectionFactory.CreateConnection();
            try
            {
                return await connection.ExecuteScalarAsync<int>(
                    "sp_Gastos_Registrar",
                    new
                    {
                        gasto.AulaId,
                        gasto.Concepto,
                        gasto.Categoria,
                        gasto.Monto,
                        gasto.FechaGasto,
                        gasto.TipoComprobante,
                        gasto.NumeroComprobante,
                        gasto.Proveedor,
                        gasto.Observacion,
                        gasto.UrlComprobante,
                        gasto.UsuarioRegistro
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

        public async Task<IEnumerable<GastoComite>> ObtenerPorAulaAsync(int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<GastoComite>(
                "sp_Gastos_ObtenerPorAula",
                new { AulaId = aulaId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<ResumenCajaAulaDto> ObtenerResumenCajaAsync(int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var resumen = await connection.QueryFirstOrDefaultAsync<ResumenCajaAulaDto>(
                "sp_Gastos_ObtenerResumenCaja",
                new { AulaId = aulaId },
                commandType: CommandType.StoredProcedure
            );

            return resumen ?? new ResumenCajaAulaDto();
        }

        public async Task<bool> ActualizarAsync(GastoComite gasto, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? _connectionFactory.CreateConnection();
            try
            {
                var filasAfectadas = await connection.ExecuteScalarAsync<int>(
                    "sp_Gastos_Actualizar",
                    new
                    {
                        gasto.Id,
                        gasto.AulaId,
                        gasto.Concepto,
                        gasto.Categoria,
                        gasto.Monto,
                        gasto.FechaGasto,
                        gasto.TipoComprobante,
                        gasto.NumeroComprobante,
                        gasto.Proveedor,
                        gasto.Observacion,
                        gasto.UrlComprobante,
                        gasto.UsuarioRegistro
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure
                );

                return filasAfectadas > 0;
            }
            finally
            {
                if (transaction == null) connection.Dispose();
            }
        }

        public async Task EliminarAsync(int gastoId, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? _connectionFactory.CreateConnection();
            try
            {
                await connection.ExecuteAsync(
                    "sp_Gastos_Eliminar",
                    new { GastoId = gastoId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure
                );
            }
            finally
            {
                if (transaction == null) connection.Dispose();
            }
        }

        public async Task<ResumenCajaAulaDto> ObtenerBalanceMensualCajaAsync(int aulaId, int anioLectivo, int? mes)
        {
            using var connection = _connectionFactory.CreateConnection();
            var resumen = await connection.QueryFirstOrDefaultAsync<ResumenCajaAulaDto>(
                "sp_Gastos_ObtenerBalanceMensualCaja",
                new { AulaId = aulaId, AnioLectivo = anioLectivo, Mes = mes },
                commandType: CommandType.StoredProcedure
            );

            return resumen ?? new ResumenCajaAulaDto();
        }

        public async Task<GastoComite?> ObtenerPorIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT * FROM GastosComite WHERE Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<GastoComite>(sql, new { Id = id });
        }
    }
}
