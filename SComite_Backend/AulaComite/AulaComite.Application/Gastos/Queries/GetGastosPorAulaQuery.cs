using AulaComite.Application.Gastos.Dtos;
using MediatR;

namespace AulaComite.Application.Gastos.Queries
{
    public record GetGastosPorAulaQuery(int AulaId) : IRequest<IEnumerable<GastoComiteDto>>;
}