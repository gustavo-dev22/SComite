using AulaComite.Application.Estudiantes.Dtos;
using MediatR;

namespace AulaComite.Application.Estudiantes.Queries
{
    public record GetEstudianteByIdQuery(int EstudianteId) : IRequest<EstudianteDto?>;
}