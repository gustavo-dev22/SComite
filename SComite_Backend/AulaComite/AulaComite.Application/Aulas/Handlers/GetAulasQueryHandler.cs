using AulaComite.Application.Aulas.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Aulas.Handlers
{
    public class GetAulasQueryHandler : IRequestHandler<GetAulasQuery, IEnumerable<Aula>>
    {
        private readonly IAulaRepository _aulaRepository;

        public GetAulasQueryHandler(IAulaRepository aulaRepository)
        {
            _aulaRepository = aulaRepository;
        }

        public async Task<IEnumerable<Aula>> Handle(GetAulasQuery request, CancellationToken cancellationToken)
        {
            return await _aulaRepository.ObtenertodasAsync(request.PeriodoId);
        }
    }
}
