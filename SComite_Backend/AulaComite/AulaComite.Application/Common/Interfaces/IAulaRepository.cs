using AulaComite.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IAulaRepository
    {
        Task<IEnumerable<Aula>> ObtenertodasAsync(int? periodoId);
        Task<IEnumerable<PeriodoLectivo>> ObtenerPeriodosAsync();
        Task<int> CrearAulaAsync(Aula aula);
        Task<bool> ActualizarEstadoAulaAsync(int id, bool estado);
        Task<bool> ActualizarAulaAsync(Aula aula);
        Task<bool> EliminarAulaLogicoAsync(int id);
        Task<Aula?> ObtenerPorIdAsync(int id);
    }
}
