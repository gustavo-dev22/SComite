using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Actividades.Commands;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Actividades.Handlers
{
    public class EliminarActividadCommandHandler : IRequestHandler<EliminarActividadCommand, bool>
    {
        private readonly IActividadRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public EliminarActividadCommandHandler(IActividadRepository repository, IComiteRepository comiteRepository, IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(EliminarActividadCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ T4/IDOR: se verifica PRIMERO la existencia del recurso. Si no existe -> 404 (false).
            var actividad = await _repository.ObtenerPorIdAsync(request.Id);
            if (actividad == null)
                return false;

            // 🛡️ Se valida el AulaId REAL del recurso (nunca el AulaId enviado por el cliente),
            // de modo que un usuario sin acceso al Aula de la actividad reciba 403.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, actividad.AulaId);

            return await _repository.EliminarAsync(request.Id, actividad.AulaId);
        }
    }
}
