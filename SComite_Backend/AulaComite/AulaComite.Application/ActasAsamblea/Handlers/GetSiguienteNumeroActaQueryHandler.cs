using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.ActasAsamblea.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Handlers
{
    public class GetSiguienteNumeroActaQueryHandler : IRequestHandler<GetSiguienteNumeroActaQuery, string>
    {
        private readonly IActaAsambleaRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetSiguienteNumeroActaQueryHandler(
            IActaAsambleaRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<string> Handle(GetSiguienteNumeroActaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            return await _repository.ObtenerSiguienteNumeroActaAsync(request.AulaId, request.AnioLectivo);
        }
    }
}