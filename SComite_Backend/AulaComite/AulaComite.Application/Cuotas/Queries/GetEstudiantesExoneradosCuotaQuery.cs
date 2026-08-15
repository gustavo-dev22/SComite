using AulaComite.Application.Cuotas.Dtos;
using MediatR;

namespace AulaComite.Application.Cuotas.Queries
{
    public record GetEstudiantesExoneradosCuotaQuery(int CuotaId) : IRequest<List<EstudianteExoneradoCuotaDto>>;
}
