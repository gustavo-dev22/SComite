using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Balance.Dtos;
using AulaComite.Application.Balance.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using AulaComite.Domain.Entities;

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

            // 🚀 Cargar los egresos detallados con concepto
            var gastosDetalle = await _repository.ObtenerGastosDetalleAsync(request.AulaId, request.AnioLectivo, request.Mes);

            // Retornar la DTO con sus 3 parámetros completos
            return new BalanceGeneralDTO(consolidado, categorias, gastosDetalle);
        }
    }
}
