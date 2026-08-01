using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.ActasAsamblea.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Handlers
{
    public class GetSiguienteNumeroActaQueryHandler : IRequestHandler<GetSiguienteNumeroActaQuery, string>
    {
        private readonly IActaAsambleaRepository _repository;

        public GetSiguienteNumeroActaQueryHandler(IActaAsambleaRepository repository) => _repository = repository;

        public async Task<string> Handle(GetSiguienteNumeroActaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerSiguienteNumeroActaAsync(request.AulaId, request.AnioLectivo);
        }
    }
}
