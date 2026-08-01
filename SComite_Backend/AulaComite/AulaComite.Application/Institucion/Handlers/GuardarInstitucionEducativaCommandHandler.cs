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

        public GuardarInstitucionEducativaCommandHandler(IInstitucionRepository repository) => _repository = repository;

        public async Task<bool> Handle(GuardarInstitucionEducativaCommand request, CancellationToken cancellationToken)
        {
            var entidad = new InstitucionEducativa
            {
                NombreInstitucion = request.NombreInstitucion,
                Direccion = request.Direccion,
                UrlLogo = request.UrlLogo,
                UsuarioActualizacion = request.UsuarioActualizacion
            };

            return await _repository.GuardarAsync(entidad);
        }
    }
}
