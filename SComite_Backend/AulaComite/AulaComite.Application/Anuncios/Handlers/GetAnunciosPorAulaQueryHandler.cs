using AulaComite.Application.Anuncios.Dtos;
using AulaComite.Application.Anuncios.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Anuncios.Handlers
{
    /// <summary>
    /// 🚀 T3.5: Listado de anuncios del muro por aula. Soporte volumétrico actual:
    /// &lt;100 registros por aula (se devuelve IEnumerable sin paginar). El DTO queda
    /// preparado para migrar a una paginación futura (PagedResultDto&lt;T&gt;).
    /// </summary>
    public class GetAnunciosPorAulaQueryHandler : IRequestHandler<GetAnunciosPorAulaQuery, IEnumerable<AnuncioComiteDto>>
    {
        private readonly IAnuncioRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetAnunciosPorAulaQueryHandler(
            IAnuncioRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<IEnumerable<AnuncioComiteDto>> Handle(GetAnunciosPorAulaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

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