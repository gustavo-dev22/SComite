using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Donaciones.Dtos;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IDonacionRepository
    {
        Task<IEnumerable<DonacionDto>> ObtenerPorAulaAsync(int aulaId, int anioLectivo, int? mes);
        Task<int> GuardarAsync(int id, int aulaId, string donante, decimal monto, DateTime fechaDonacion, string concepto, string? observacion);
        Task<bool> EliminarAsync(int id, int aulaId);
    }
}
