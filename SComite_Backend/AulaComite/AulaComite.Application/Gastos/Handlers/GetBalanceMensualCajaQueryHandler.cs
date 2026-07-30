using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Gastos.Queries;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Gastos.Handlers
{
    public class GetBalanceMensualCajaQueryHandler : IRequestHandler<GetBalanceMensualCajaQuery, ResumenCajaAula>
    {
        private readonly IGastoRepository _repository;

        public GetBalanceMensualCajaQueryHandler(IGastoRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResumenCajaAula> Handle(GetBalanceMensualCajaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerBalanceMensualCajaAsync(request.AulaId, request.AnioLectivo, request.Mes);
        }
    }
}
