using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Comite.Commands;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Comite.Handlers
{
    public class DeleteComiteCommandHandler : IRequestHandler<DeleteComiteCommand, bool>
    {
        private readonly IComiteRepository _repository;
        private readonly IUserContextService _userContextService;

        public DeleteComiteCommandHandler(IComiteRepository repository, IUserContextService userContextService)
        {
            _repository = repository;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(DeleteComiteCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: el integrante debe pertenecer a un Aula del usuario autenticado.
            var integrante = await _repository.ObtenerIntegrantePorIdAsync(request.Id);
            if (integrante == null) return false;

            await AulaAccessValidator.ValidarAccesoAulaAsync(_repository, _userContextService, integrante.AulaId);

            return await _repository.EliminarIntegranteAsync(request.Id);
        }
    }
}
