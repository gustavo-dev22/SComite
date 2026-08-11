using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Gastos.Dtos;
using AulaComite.Application.Gastos.Queries;
using MediatR;

namespace AulaComite.Application.Gastos.Handlers
{
    public class GetBalanceMensualCajaQueryHandler : IRequestHandler<GetBalanceMensualCajaQuery, ResumenCajaAulaDto>
    {
        private readonly IGastoRepository _repository;

        public GetBalanceMensualCajaQueryHandler(IGastoRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResumenCajaAulaDto> Handle(GetBalanceMensualCajaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerBalanceMensualCajaAsync(request.AulaId, request.AnioLectivo, request.Mes);
        }
    }
}
