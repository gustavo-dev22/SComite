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
            // 🛡️ T4/IDOR: se verifica PRIMERO la existencia del recurso. Si no existe -> 404 (false).
            var acta = await _repository.ObtenerPorIdAsync(request.Id);
            if (acta == null)
                return false;

            // 🛡️ Se valida el AulaId REAL del recurso (nunca el AulaId enviado por el cliente),
            // de modo que un usuario sin acceso al Aula del acta reciba 403.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, acta.AulaId);

            return await _repository.EliminarAsync(request.Id, acta.AulaId);
        }
    }
}
