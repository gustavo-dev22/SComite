using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using AulaComite.Application.Gastos.Dtos;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IGastoRepository
    {
        Task<int> RegistrarAsync(GastoComite gasto, IDbTransaction? transaction = null);
        Task<bool> ActualizarAsync(GastoComite gasto, IDbTransaction? transaction = null);
        Task<IEnumerable<GastoComite>> ObtenerPorAulaAsync(int aulaId);
        Task<ResumenCajaAulaDto> ObtenerResumenCajaAsync(int aulaId);
        Task EliminarAsync(int gastoId, IDbTransaction? transaction = null);
        Task<ResumenCajaAulaDto> ObtenerBalanceMensualCajaAsync(int aulaId, int anioLectivo, int? mes);
        Task<GastoComite?> ObtenerPorIdAsync(int id);
    }
}
