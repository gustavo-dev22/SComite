using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Donaciones.Commands;
using MediatR;

namespace AulaComite.Application.Donaciones.Handlers
{
    public class EliminarDonacionCommandHandler : IRequestHandler<EliminarDonacionCommand, bool>
    {
        private readonly IDonacionRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public EliminarDonacionCommandHandler(IDonacionRepository repository, IComiteRepository comiteRepository, IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(EliminarDonacionCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ T4/IDOR: se verifica PRIMERO la existencia del recurso. Si no existe -> 404 (false).
            var donacion = await _repository.ObtenerPorIdAsync(request.Id);
            if (donacion == null)
                return false;

            // 🛡️ Se valida el AulaId REAL del recurso (nunca el AulaId enviado por el cliente),
            // de modo que un usuario sin acceso al Aula de la donación reciba 403.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, donacion.AulaId);

            return await _repository.EliminarAsync(request.Id, donacion.AulaId);
        }
    }
}
