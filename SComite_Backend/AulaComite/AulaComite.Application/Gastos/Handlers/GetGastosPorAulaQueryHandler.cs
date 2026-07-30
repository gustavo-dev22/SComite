using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Gastos.Queries;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Gastos.Handlers
{
    public class GetGastosPorAulaQueryHandler : IRequestHandler<GetGastosPorAulaQuery, IEnumerable<GastoComite>>
    {
        private readonly IGastoRepository _repository;

        public GetGastosPorAulaQueryHandler(IGastoRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<GastoComite>> Handle(GetGastosPorAulaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerPorAulaAsync(request.AulaId);
        }
    }
}
