using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Anuncios.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Anuncios.Handlers
{
    public class GetAnunciosPorAulaQueryHandler : IRequestHandler<GetAnunciosPorAulaQuery, IEnumerable<AnuncioComite>>
    {
        private readonly IAnuncioRepository _repository;

        public GetAnunciosPorAulaQueryHandler(IAnuncioRepository repository) => _repository = repository;

        public async Task<IEnumerable<AnuncioComite>> Handle(GetAnunciosPorAulaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerPorAulaAsync(request.AulaId, request.AnioLectivo);
        }
    }
}
