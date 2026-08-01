using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.ActasAsamblea.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.ActasAsamblea.Handlers
{
    public class GetActasPorAulaQueryHandler : IRequestHandler<GetActasPorAulaQuery, IEnumerable<ActaAsambleaComite>>
    {
        private readonly IActaAsambleaRepository _repository;

        public GetActasPorAulaQueryHandler(IActaAsambleaRepository repository) => _repository = repository;

        public async Task<IEnumerable<ActaAsambleaComite>> Handle(GetActasPorAulaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerPorAulaAsync(request.AulaId, request.AnioLectivo);
        }
    }
}
