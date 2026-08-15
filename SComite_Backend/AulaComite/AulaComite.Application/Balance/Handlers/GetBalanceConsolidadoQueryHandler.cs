using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AulaComite.Application.Balance.Dtos;
using AulaComite.Application.Balance.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Balance.Handlers
{
    public class GetBalanceConsolidadoQueryHandler : IRequestHandler<GetBalanceConsolidadoQuery, BalanceGeneralDto>
    {
        private readonly IBalanceRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetBalanceConsolidadoQueryHandler(
            IBalanceRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<BalanceGeneralDto> Handle(GetBalanceConsolidadoQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            var consolidado = await _repository.ObtenerConsolidadoAsync(request.AulaId, request.AnioLectivo, request.Mes);
            var categorias = await _repository.ObtenerGastosPorCategoriaAsync(request.AulaId, request.AnioLectivo, request.Mes);
            var gastosDetalle = await _repository.ObtenerGastosDetalleAsync(request.AulaId, request.AnioLectivo, request.Mes);

            return new BalanceGeneralDto(
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
                categorias,
                gastosDetalle
            );
        }
    }
}