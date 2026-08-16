using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Gastos.Dtos;
using AulaComite.Application.Gastos.Queries;
using MediatR;

namespace AulaComite.Application.Gastos.Handlers
{
    /// <summary>
    /// 🚀 T3.5: Listado de gastos por aula. Límite defensivo de 200 registros
    /// para evitar sobrecarga de memoria (OOM) en respuestas masivas.
    /// </summary>
    public class GetGastosPorAulaQueryHandler : IRequestHandler<GetGastosPorAulaQuery, IEnumerable<GastoComiteDto>>
    {
        private const int LimiteMaximoRegistros = 200;

        private readonly IGastoRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetGastosPorAulaQueryHandler(
            IGastoRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<IEnumerable<GastoComiteDto>> Handle(GetGastosPorAulaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            var gastos = await _repository.ObtenerPorAulaAsync(request.AulaId);

            // 🚀 T5: Límite defensivo de volumen para prevenir OOM en listados masivos.
            return gastos.Take(LimiteMaximoRegistros).Select(g => new GastoComiteDto
            {
                Id = g.Id,
                AulaId = g.AulaId,
                Concepto = g.Concepto,
                Categoria = g.Categoria,
                Monto = g.Monto,
                FechaGasto = g.FechaGasto,
                TipoComprobante = g.TipoComprobante,
                NumeroComprobante = g.NumeroComprobante,
                UrlComprobante = g.UrlComprobante,
                Proveedor = g.Proveedor,
                Observacion = g.Observacion,
                UsuarioRegistro = g.UsuarioRegistro,
                FechaRegistro = g.FechaRegistro
            });
        }
    }
}