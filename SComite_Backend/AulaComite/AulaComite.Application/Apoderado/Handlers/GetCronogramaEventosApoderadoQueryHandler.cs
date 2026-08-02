using AulaComite.Application.Apoderado.Dtos;
using AulaComite.Application.Apoderado.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Handlers
{
    public class GetCronogramaEventosApoderadoQueryHandler
    : IRequestHandler<GetCronogramaEventosApoderadoQuery, List<EventoCronogramaApoderadoDto>>
    {
        private readonly IApoderadoRepository _repository;

        public GetCronogramaEventosApoderadoQueryHandler(IApoderadoRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<EventoCronogramaApoderadoDto>> Handle(
            GetCronogramaEventosApoderadoQuery request,
            CancellationToken cancellationToken)
        {
            var result = await _repository.ObtenerCronogramaEventosAsync(request.EstudianteId, request.AnioLectivo);
            return result.ToList();
        }
    }
}
