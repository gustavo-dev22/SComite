using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Estudiantes.Dtos;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IEstudianteRepository
    {
        Task<IEnumerable<Estudiante>> ObtenerPorAulaAsync(int aulaId);
        Task<int> CrearEstudianteAsync(Estudiante estudiante);
        Task<bool> ActualizarEstudianteAsync(Estudiante estudiante);
        Task<bool> EliminarEstudianteLogicoAsync(int id);
        Task<int> CargaMasivaEstudiantesAsync(int aulaId, IEnumerable<EstudianteImportacionItemDto> listaEstudiantes);
    }
}
