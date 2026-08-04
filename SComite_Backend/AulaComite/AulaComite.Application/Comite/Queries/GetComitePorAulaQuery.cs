using AulaComite.Application.Comite.Dtos;
using MediatR;

namespace AulaComite.Application.Comite.Queries
{
    public record GetComitePorAulaQuery(int AulaId) : IRequest<IEnumerable<ComiteIntegranteDto>>;
}