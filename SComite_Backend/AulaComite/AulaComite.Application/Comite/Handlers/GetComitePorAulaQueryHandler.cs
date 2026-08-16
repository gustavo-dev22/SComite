using AulaComite.Application.Comite.Dtos;
using AulaComite.Application.Comite.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Comite.Handlers
{
    /// <summary>
    /// 🚀 T3.5: Listado de integrantes del comité por aula. Soporte volumétrico actual:
    /// &lt;100 registros por aula (se devuelve IEnumerable sin paginar). El DTO queda
    /// preparado para migrar a una paginación futura (PagedResultDto&lt;T&gt;).
    /// </summary>
    public class GetComitePorAulaQueryHandler : IRequestHandler<GetComitePorAulaQuery, IEnumerable<ComiteIntegranteDto>>
    {
        private readonly IComiteRepository _repository;
        private readonly IUserContextService _userContextService;

        public GetComitePorAulaQueryHandler(
            IComiteRepository repository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _userContextService = userContextService;
        }

        public async Task<IEnumerable<ComiteIntegranteDto>> Handle(GetComitePorAulaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_repository, _userContextService, request.AulaId);

            var integrantes = await _repository.ObtenerPorAulaAsync(request.AulaId);

            return integrantes.Select(i => new ComiteIntegranteDto
            {
                Id = i.Id,
                AulaId = i.AulaId,
                UsuarioIdSasi = i.UsuarioIdSasi,
                NombreCompleto = i.NombreCompleto,
                Email = i.Email,
                Cargo = i.Cargo,
                Estado = i.Estado,
                FechaAsignacion = i.FechaAsignacion
            });
        }
    }
}