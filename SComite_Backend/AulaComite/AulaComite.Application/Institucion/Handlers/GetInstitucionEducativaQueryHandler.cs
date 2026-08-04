using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Institucion.Dtos;
using AulaComite.Application.Institucion.Queries;
using MediatR;

namespace AulaComite.Application.Institucion.Handlers
{
    public class GetInstitucionEducativaQueryHandler : IRequestHandler<GetInstitucionEducativaQuery, InstitucionEducativaDto?>
    {
        private readonly IInstitucionRepository _repository;

        public GetInstitucionEducativaQueryHandler(IInstitucionRepository repository) => _repository = repository;

        public async Task<InstitucionEducativaDto?> Handle(GetInstitucionEducativaQuery request, CancellationToken cancellationToken)
        {
            var institucion = await _repository.ObtenerAsync();

            if (institucion == null)
            {
                return null;
            }

            return new InstitucionEducativaDto
            {
                Id = institucion.Id,
                NombreInstitucion = institucion.NombreInstitucion,
                Direccion = institucion.Direccion,
                UrlLogo = institucion.UrlLogo,
                FechaActualizacion = institucion.FechaActualizacion,
                UsuarioActualizacion = institucion.UsuarioActualizacion
            };
        }
    }
}