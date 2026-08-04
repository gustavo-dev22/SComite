using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AulaComite.Application.Balance.Dtos;
using AulaComite.Application.Balance.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Balance.Handlers
{
    public class GetBalanceConsolidadoQueryHandler : IRequestHandler<GetBalanceConsolidadoQuery, BalanceGeneralDTO>
    {
        private readonly IBalanceRepository _repository;

        public GetBalanceConsolidadoQueryHandler(IBalanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<BalanceGeneralDTO> Handle(GetBalanceConsolidadoQuery request, CancellationToken cancellationToken)
        {
            var consolidado = await _repository.ObtenerConsolidadoAsync(request.AulaId, request.AnioLectivo, request.Mes);
            var categorias = await _repository.ObtenerGastosPorCategoriaAsync(request.AulaId, request.AnioLectivo, request.Mes);
            var gastosDetalle = await _repository.ObtenerGastosDetalleAsync(request.AulaId, request.AnioLectivo, request.Mes);

            return new BalanceGeneralDTO(
                new BalanceConsolidadoDto
                {
                    SaldoAnteriorArrastrado = consolidado.SaldoAnteriorArrastrado,
                    IngresosMensuales = consolidado.IngresosMensuales,
                    IngresosExtraordinarios = consolidado.IngresosExtraordinarios,
                    IngresosDonaciones = consolidado.IngresosDonaciones,
                    TotalIngresosMes = consolidado.TotalIngresosMes,
                    TotalEgresosMes = consolidado.TotalEgresosMes,
                    SaldoNetoEnCaja = consolidado.SaldoNetoEnCaja,
                    TotalPorCobrar = consolidado.TotalPorCobrar,
                    PorcentajeCumplimiento = consolidado.PorcentajeCumplimiento
                },
                categorias.Select(c => new GastoCategoriaResumenDto
                {
                    Categoria = c.Categoria,
                    TotalMonto = c.TotalMonto,
                    CantidadRegistros = c.CantidadRegistros
                }),
                gastosDetalle
            );
        }
    }
}