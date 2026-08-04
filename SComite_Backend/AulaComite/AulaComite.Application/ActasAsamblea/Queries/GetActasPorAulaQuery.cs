using AulaComite.Application.ActasAsamblea.Dtos;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Queries
{
    public record GetActasPorAulaQuery(int AulaId, int AnioLectivo) : IRequest<IEnumerable<ActaAsambleaComiteDto>>;
}