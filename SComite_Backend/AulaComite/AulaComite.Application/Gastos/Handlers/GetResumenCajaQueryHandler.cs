using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Gastos.Queries;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Gastos.Handlers
{
    public class GetResumenCajaQueryHandler : IRequestHandler<GetResumenCajaQuery, ResumenCajaAula>
    {
        private readonly IGastoRepository _repository;

        public GetResumenCajaQueryHandler(IGastoRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResumenCajaAula> Handle(GetResumenCajaQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerResumenCajaAsync(request.AulaId);
        }
    }
}
