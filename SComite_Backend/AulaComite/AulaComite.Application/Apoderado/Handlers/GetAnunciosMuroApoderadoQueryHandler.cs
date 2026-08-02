using AulaComite.Application.Apoderado.Dtos;
using AulaComite.Application.Apoderado.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Handlers
{
    public class GetAnunciosMuroApoderadoQueryHandler : IRequestHandler<GetAnunciosMuroApoderadoQuery, List<AnuncioApoderadoDto>>
    {
        private readonly IApoderadoRepository _repository;

        public GetAnunciosMuroApoderadoQueryHandler(IApoderadoRepository repository) => _repository = repository;

        public async Task<List<AnuncioApoderadoDto>> Handle(GetAnunciosMuroApoderadoQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.ObtenerAnunciosMuroAsync(request.EstudianteId, request.AnioLectivo);
            return result.ToList();
        }
    }
}
