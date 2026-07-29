using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Estudiantes.Queries;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Estudiantes.Handlers
{
    public class GetEstudiantesPorAulaQueryHandler : IRequestHandler<GetEstudiantesPorAulaQuery, IEnumerable<Estudiante>>
    {
        private readonly IEstudianteRepository _repository;

        public GetEstudiantesPorAulaQueryHandler(IEstudianteRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Estudiante>> Handle(GetEstudiantesPorAulaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerPorAulaAsync(request.AulaId);
        }
    }
}
