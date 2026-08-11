using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Actividades.Dtos;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IActividadRepository
    {
        Task<IEnumerable<ActividadComiteDto>> ObtenerPorAulaAsync(int aulaId, int anioLectivo);
        Task<int> GuardarAsync(int id, int aulaId, string nombreActividad, string? descripcion, DateTime fechaProgramada, decimal montoPresupuestado, decimal cuotaSugeridaPorAlumno, string estado);
        Task<bool> EliminarAsync(int id, int aulaId);
    }
}
