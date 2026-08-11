using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Donaciones.Dtos;
using AulaComite.Application.Donaciones.Queries;
using MediatR;

namespace AulaComite.Application.Donaciones.Handlers
{
    public class GetDonacionesPorAulaQueryHandler : IRequestHandler<GetDonacionesPorAulaQuery, IEnumerable<DonacionDto>>
    {
        private readonly IDonacionRepository _repository;

        public GetDonacionesPorAulaQueryHandler(IDonacionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DonacionDto>> Handle(GetDonacionesPorAulaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerPorAulaAsync(request.AulaId, request.AnioLectivo, request.Mes);
        }
    }
}
