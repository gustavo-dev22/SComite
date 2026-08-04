using AulaComite.Application.Aulas.Dtos;
using MediatR;

namespace AulaComite.Application.Aulas.Queries
{
    public record GetAulasQuery(int? PeriodoId) : IRequest<IEnumerable<AulaDto>>;
}