using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Cuotas.Dtos;
using AulaComite.Application.Cuotas.Queries;
using MediatR;

namespace AulaComite.Application.Cuotas.Handlers
{
    /// <summary>
    /// 🚀 T3.5: Listado de cuotas por aula. Soporte volumétrico actual:
    /// &lt;100 registros por aula (se devuelve IEnumerable sin paginar). El DTO queda
    /// preparado para migrar a una paginación futura (PagedResultDto&lt;T&gt;).
    /// </summary>
    public class GetCuotasPorAulaQueryHandler : IRequestHandler<GetCuotasPorAulaQuery, IEnumerable<CuotaDto>>
    {
        private readonly ICuotaRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetCuotasPorAulaQueryHandler(
            ICuotaRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<IEnumerable<CuotaDto>> Handle(GetCuotasPorAulaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            return await _repository.ObtenerPorAulaAsync(request.AulaId);
        }
    }
}