using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Institucion.Queries;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Institucion.Handlers
{
    public class GetInstitucionEducativaQueryHandler : IRequestHandler<GetInstitucionEducativaQuery, InstitucionEducativa?>
    {
        private readonly IInstitucionRepository _repository;

        public GetInstitucionEducativaQueryHandler(IInstitucionRepository repository) => _repository = repository;

        public async Task<InstitucionEducativa?> Handle(GetInstitucionEducativaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerAsync();
        }
    }
}
