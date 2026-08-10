using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Actividades.Commands;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Actividades.Handlers
{
    public class GuardarActividadCommandHandler : IRequestHandler<GuardarActividadCommand, int>
    {
        private readonly IActividadRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GuardarActividadCommandHandler(IActividadRepository repository, IComiteRepository comiteRepository, IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<int> Handle(GuardarActividadCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: la actividad debe corresponder a un Aula asignada al usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            return await _repository.GuardarAsync(
                request.Id,
                request.AulaId,
                request.NombreActividad,
                request.Descripcion,
                request.FechaProgramada,
                request.MontoPresupuestado,
                request.CuotaSugeridaPorAlumno,
                request.Estado
            );
        }
    }
}
