using AulaComite.Application.Cuotas.Dtos;
using MediatR;

namespace AulaComite.Application.Cuotas.Queries
{
    public record GetCuotasPorAulaQuery(int AulaId) : IRequest<IEnumerable<CuotaDto>>;
}