using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Gastos.Dtos;
using AulaComite.Application.Gastos.Queries;
using MediatR;

namespace AulaComite.Application.Gastos.Handlers
{
    public class GetGastosPorAulaQueryHandler : IRequestHandler<GetGastosPorAulaQuery, IEnumerable<GastoComiteDto>>
    {
        private readonly IGastoRepository _repository;

        public GetGastosPorAulaQueryHandler(IGastoRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<GastoComiteDto>> Handle(GetGastosPorAulaQuery request, CancellationToken cancellationToken)
        {
            var gastos = await _repository.ObtenerPorAulaAsync(request.AulaId);

            return gastos.Select(g => new GastoComiteDto
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