using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Balance.Dtos;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IBalanceRepository
    {
        Task<BalanceConsolidado> ObtenerConsolidadoAsync(int aulaId, int anioLectivo, int? mes);
        Task<IEnumerable<GastoCategoriaResumen>> ObtenerGastosPorCategoriaAsync(int aulaId, int anioLectivo, int? mes);
        Task<IEnumerable<GastoComiteDTO>> ObtenerGastosDetalleAsync(int aulaId, int anioLectivo, int? mes);
    }
}
