using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Application.Common.Interfaces;
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

        public async Task<int> RegistrarAsync(GastoComite gasto)
        {
            using var connection = _connectionFactory.CreateConnection();
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
                    gasto.UsuarioRegistro
                },
                commandType: CommandType.StoredProcedure
            );
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

        public async Task<ResumenCajaAula> ObtenerResumenCajaAsync(int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var resumen = await connection.QueryFirstOrDefaultAsync<ResumenCajaAula>(
                "sp_Gastos_ObtenerResumenCaja",
                new { AulaId = aulaId },
                commandType: CommandType.StoredProcedure
            );

            return resumen ?? new ResumenCajaAula();
        }

        public async Task EliminarAsync(int gastoId)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                "sp_Gastos_Eliminar",
                new { GastoId = gastoId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<ResumenCajaAula> ObtenerBalanceMensualCajaAsync(int aulaId, int anioLectivo, int? mes)
        {
            using var connection = _connectionFactory.CreateConnection();
            var resumen = await connection.QueryFirstOrDefaultAsync<ResumenCajaAula>(
                "sp_Gastos_ObtenerBalanceMensualCaja",
                new { AulaId = aulaId, AnioLectivo = anioLectivo, Mes = mes },
                commandType: CommandType.StoredProcedure
            );

            return resumen ?? new ResumenCajaAula();
        }
    }
}
