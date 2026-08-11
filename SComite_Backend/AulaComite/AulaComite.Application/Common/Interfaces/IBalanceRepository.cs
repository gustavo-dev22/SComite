using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Balance.Dtos;
using AulaComite.Application.Gastos.Dtos;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IBalanceRepository
    {
        Task<BalanceConsolidado> ObtenerConsolidadoAsync(int aulaId, int anioLectivo, int? mes);
        Task<IEnumerable<GastoCategoriaResumenDto>> ObtenerGastosPorCategoriaAsync(int aulaId, int anioLectivo, int? mes);
        Task<IEnumerable<GastoComiteDto>> ObtenerGastosDetalleAsync(int aulaId, int anioLectivo, int? mes);
    }
}
