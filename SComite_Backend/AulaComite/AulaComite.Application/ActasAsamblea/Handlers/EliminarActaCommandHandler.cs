using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.ActasAsamblea.Commands;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Handlers
{
    public class EliminarActaCommandHandler : IRequestHandler<EliminarActaCommand, bool>
    {
        private readonly IActaAsambleaRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public EliminarActaCommandHandler(IActaAsambleaRepository repository, IComiteRepository comiteRepository, IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(EliminarActaCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: el acta debe pertenecer a un Aula asignada al usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            return await _repository.EliminarAsync(request.Id, request.AulaId);
        }
    }
}
