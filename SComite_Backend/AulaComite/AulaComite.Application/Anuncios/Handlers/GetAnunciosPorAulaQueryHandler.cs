using AulaComite.Application.Anuncios.Dtos;
using AulaComite.Application.Anuncios.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Anuncios.Handlers
{
    public class GetAnunciosPorAulaQueryHandler : IRequestHandler<GetAnunciosPorAulaQuery, IEnumerable<AnuncioComiteDto>>
    {
        private readonly IAnuncioRepository _repository;

        public GetAnunciosPorAulaQueryHandler(IAnuncioRepository repository) => _repository = repository;

        public async Task<IEnumerable<AnuncioComiteDto>> Handle(GetAnunciosPorAulaQuery request, CancellationToken cancellationToken)
        {
            var anuncios = await _repository.ObtenerPorAulaAsync(request.AulaId, request.AnioLectivo);

            return anuncios.Select(a => new AnuncioComiteDto
            {
                Id = a.Id,
                AulaId = a.AulaId,
                Titulo = a.Titulo,
                Contenido = a.Contenido,
                Categoria = a.Categoria,
                EsFijado = a.EsFijado,
                UrlAdjunto = a.UrlAdjunto,
                UsuarioRegistro = a.UsuarioRegistro,
                FechaPublicacion = a.FechaPublicacion,
                CantidadVistas = a.CantidadVistas,
                Estado = a.Estado
            });
        }
    }
}