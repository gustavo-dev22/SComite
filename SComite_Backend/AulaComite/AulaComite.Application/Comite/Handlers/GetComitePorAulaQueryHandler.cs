using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Comite.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Comite.Handlers
{
    public class GetComitePorAulaQueryHandler : IRequestHandler<GetComitePorAulaQuery, IEnumerable<ComiteIntegrante>>
    {
        private readonly IComiteRepository _repository;

        public GetComitePorAulaQueryHandler(IComiteRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ComiteIntegrante>> Handle(GetComitePorAulaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerPorAulaAsync(request.AulaId);
        }
    }
}
