using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Apoderado.Dtos;
using AulaComite.Application.Apoderado.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Apoderado.Handlers
{
    public class GetActasAprobadasApoderadoQueryHandler
    : IRequestHandler<GetActasAprobadasApoderadoQuery, List<ActaApoderadoDto>>
    {
        private readonly IApoderadoRepository _repository;

        public GetActasAprobadasApoderadoQueryHandler(IApoderadoRepository repository) => _repository = repository;

        public async Task<List<ActaApoderadoDto>> Handle(
            GetActasAprobadasApoderadoQuery request,
            CancellationToken cancellationToken)
        {
            var result = await _repository.ObtenerActasAprobadasAsync(request.EstudianteId, request.AnioLectivo);
            return result.ToList();
        }
    }
}
