using AulaComite.Application.Anuncios.Dtos;
using MediatR;

namespace AulaComite.Application.Anuncios.Queries
{
    public record GetAnunciosPorAulaQuery(int AulaId, int AnioLectivo) : IRequest<IEnumerable<AnuncioComiteDto>>;
}