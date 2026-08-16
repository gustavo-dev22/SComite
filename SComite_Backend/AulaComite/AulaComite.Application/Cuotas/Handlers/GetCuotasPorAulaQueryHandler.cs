using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Cuotas.Dtos;
using AulaComite.Application.Cuotas.Queries;
using MediatR;

namespace AulaComite.Application.Cuotas.Handlers
{
    /// <summary>
    /// 🚀 T3.5: Listado de cuotas por aula. Límite defensivo de 200 registros
    /// para evitar sobrecarga de memoria (OOM) en respuestas masivas.
    /// </summary>
    public class GetCuotasPorAulaQueryHandler : IRequestHandler<GetCuotasPorAulaQuery, IEnumerable<CuotaDto>>
    {
        private const int LimiteMaximoRegistros = 200;

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

            // 🚀 T5: Límite defensivo de volumen para prevenir OOM en listados masivos.
            var cuotas = await _repository.ObtenerPorAulaAsync(request.AulaId);
            return cuotas.Take(LimiteMaximoRegistros);
        }
    }
}