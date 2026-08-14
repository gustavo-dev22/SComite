using MediatR;

namespace AulaComite.Application.Aulas.Queries
{
    public record GetMisAulasQuery(int? PeriodoId) : IRequest<IEnumerable<AulaComite.Application.Aulas.Dtos.AulaDto>>;
}