using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Actividades.Dtos;
using AulaComite.Application.Actividades.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Actividades.Handlers
{
    public class GetActividadesPorAulaQueryHandler : IRequestHandler<GetActividadesPorAulaQuery, IEnumerable<ActividadComiteDto>>
    {
        private readonly IActividadRepository _repository;

        public GetActividadesPorAulaQueryHandler(IActividadRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ActividadComiteDto>> Handle(GetActividadesPorAulaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerPorAulaAsync(request.AulaId, request.AnioLectivo);
        }
    }
}
