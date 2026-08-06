using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Aulas.Dtos;
using AulaComite.Application.Aulas.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AulaComite.Application.Aulas.Handlers
{
    public class GetBalanceAulaQueryHandler : IRequestHandler<GetBalanceAulaQuery, BalanceAulaDto>
    {
        private readonly ITransparenciaRepository _transparenciaRepository;

        public GetBalanceAulaQueryHandler(ITransparenciaRepository transparenciaRepository)
        {
            _transparenciaRepository = transparenciaRepository;
        }

        public async Task<BalanceAulaDto> Handle(GetBalanceAulaQuery request, CancellationToken cancellationToken)
        {
            return await _transparenciaRepository.ObtenerBalancePorAulaAsync(request.AulaId, request.Anio);
        }
    }
}
