using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Aulas.Dtos;
using Dapper;

namespace AulaComite.Infrastructure.Repositories
{
    public class TransparenciaRepository : ITransparenciaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TransparenciaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<BalanceAulaDto> ObtenerBalancePorAulaAsync(int aulaId, int anio)
        {
            using var connection = _connectionFactory.CreateConnection();

            using var multi = await connection.QueryMultipleAsync(
                "sp_Apoderado_ObtenerTransparenciaBalanceAula",
                new { AulaId = aulaId, Anio = anio },
                commandType: CommandType.StoredProcedure
            );

            var resumen = await multi.ReadFirstOrDefaultAsync<(decimal TotalIngresos, decimal TotalEgresos)>();
            var desgloseMensual = (await multi.ReadAsync<BalanceMensualDto>()).ToList();
            var egresos = (await multi.ReadAsync<GastoTransparenciaDto>()).ToList();

            return new BalanceAulaDto
            {
                TotalIngresos = resumen.TotalIngresos,
                TotalEgresos = resumen.TotalEgresos,
                SaldoDisponible = resumen.TotalIngresos - resumen.TotalEgresos,
                DesgloseMensual = desgloseMensual,
                Egresos = egresos
            };
        }
    }
}
