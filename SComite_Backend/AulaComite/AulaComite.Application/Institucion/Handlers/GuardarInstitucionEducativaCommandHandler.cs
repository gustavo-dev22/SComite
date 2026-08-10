using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Institucion.Commands;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Institucion.Handlers
{
    public class GuardarInstitucionEducativaCommandHandler : IRequestHandler<GuardarInstitucionEducativaCommand, bool>
    {
        private readonly IInstitucionRepository _repository;
        private readonly IUserContextService _userContextService;

        public GuardarInstitucionEducativaCommandHandler(IInstitucionRepository repository, IUserContextService userContextService)
        {
            _repository = repository;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(GuardarInstitucionEducativaCommand request, CancellationToken cancellationToken)
        {
            // Auditoría derivada exclusivamente del token JWT autenticado, nunca del cuerpo JSON.
            string usuarioActualizacion = _userContextService.ObtenerUsuarioActual();

            var entidad = new InstitucionEducativa
            {
                NombreInstitucion = request.NombreInstitucion,
                Direccion = request.Direccion,
                UrlLogo = request.UrlLogo,
                UsuarioActualizacion = usuarioActualizacion
            };

            return await _repository.GuardarAsync(entidad);
        }
    }
}
