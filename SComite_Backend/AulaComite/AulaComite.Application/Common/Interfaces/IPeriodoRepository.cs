using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IPeriodoRepository
    {
        Task<int> CrearAsync(PeriodoLectivo periodo, IDbTransaction? transaction = null);
        Task<bool> ActualizarAsync(PeriodoLectivo periodo);
        Task<bool> CambiarEstadoAsync(int id, bool esActivo);
        Task<bool> ExisteAnioAsync(int anio);
        Task<PeriodoLectivo?> ObtenerPorIdAsync(int id);
    }
}
