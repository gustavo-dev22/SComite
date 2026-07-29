using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Periodos.Commands;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Periodos.Handlers
{
    public class UpdatePeriodoCommandHandler : IRequestHandler<UpdatePeriodoCommand, bool>
    {
        private readonly IPeriodoRepository _repository;

        public UpdatePeriodoCommandHandler(IPeriodoRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(UpdatePeriodoCommand request, CancellationToken cancellationToken)
        {
            var p = new PeriodoLectivo
            {
                Id = request.Id,
                Anio = request.Anio,
                FechaInicio = request.FechaInicio,
                FechaFin = request.FechaFin,
                EsActivo = request.EsActivo
            };

            return await _repository.ActualizarAsync(p);
        }
    }
}
