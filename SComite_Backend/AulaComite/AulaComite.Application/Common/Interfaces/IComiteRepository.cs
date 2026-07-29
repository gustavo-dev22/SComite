using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IComiteRepository
    {
        Task<IEnumerable<ComiteIntegrante>> ObtenerPorAulaAsync(int aulaId);
        Task<int> AsignarIntegranteAsync(ComiteIntegrante integrante);
        Task<bool> EliminarIntegranteAsync(int id);
    }
}
