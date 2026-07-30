using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IGastoRepository
    {
        Task<int> RegistrarAsync(GastoComite gasto);
        Task<IEnumerable<GastoComite>> ObtenerPorAulaAsync(int aulaId);
        Task<ResumenCajaAula> ObtenerResumenCajaAsync(int aulaId);
        Task EliminarAsync(int gastoId);
        Task<ResumenCajaAula> ObtenerBalanceMensualCajaAsync(int aulaId, int anioLectivo, int? mes);
    }
}
