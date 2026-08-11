using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Gastos.Dtos;
using AulaComite.Application.Gastos.Queries;
using MediatR;

namespace AulaComite.Application.Gastos.Handlers
{
    public class GetResumenCajaQueryHandler : IRequestHandler<GetResumenCajaQuery, ResumenCajaAulaDto>
    {
        private readonly IGastoRepository _repository;

        public GetResumenCajaQueryHandler(IGastoRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResumenCajaAulaDto> Handle(GetResumenCajaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerResumenCajaAsync(request.AulaId);
        }
    }
}
