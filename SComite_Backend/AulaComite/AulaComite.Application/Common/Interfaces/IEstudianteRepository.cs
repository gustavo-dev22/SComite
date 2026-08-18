using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using AulaComite.Application.Estudiantes.Dtos;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IEstudianteRepository
    {
        Task<IEnumerable<Estudiante>> ObtenerPorAulaAsync(int aulaId);
        Task<Estudiante?> ObtenerPorIdAsync(int id);
        Task<int> CrearEstudianteAsync(Estudiante estudiante, IDbTransaction? transaction = null);
        Task<bool> ActualizarEstudianteAsync(Estudiante estudiante);
        Task<bool> EliminarEstudianteLogicoAsync(int id, IDbTransaction? transaction = null);
        Task<int> CargaMasivaEstudiantesAsync(int aulaId, IEnumerable<EstudianteImportacionItemDto> listaEstudiantes);
        Task<ResultadoMigracionDto> MigrarEstudiantesAsync(int aulaDestinoId, IEnumerable<int> estudianteIds);
    }
}
