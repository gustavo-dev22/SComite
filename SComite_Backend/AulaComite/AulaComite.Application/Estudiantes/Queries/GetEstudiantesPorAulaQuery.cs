using AulaComite.Application.Estudiantes.Dtos;
using MediatR;

namespace AulaComite.Application.Estudiantes.Queries
{
    public record GetEstudiantesPorAulaQuery(int AulaId) : IRequest<IEnumerable<EstudianteDto>>;
}