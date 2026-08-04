using AulaComite.Application.Cuotas.Dtos;
using MediatR;

namespace AulaComite.Application.Cuotas.Queries
{
    public record GetDetalleCobroEstudiantesQuery(int CuotaId) : IRequest<IEnumerable<CuotaEstudianteCobroDto>>;
}