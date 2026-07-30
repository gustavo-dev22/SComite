using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Cuotas.Queries;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class GetCuotasPorAulaQueryHandler : IRequestHandler<GetCuotasPorAulaQuery, IEnumerable<Cuota>>
    {
        private readonly ICuotaRepository _repository;

        public GetCuotasPorAulaQueryHandler(ICuotaRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Cuota>> Handle(GetCuotasPorAulaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerPorAulaAsync(request.AulaId);
        }
    }
}
