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
            var periodoExistente = await _repository.ObtenerPorIdAsync(request.Id);
            if (periodoExistente == null) return false;

            if (periodoExistente.Anio != request.Anio && await _repository.ExisteAnioAsync(request.Anio))
            {
                throw new FluentValidation.ValidationException($"Ya existe un Año Lectivo registrado para el año {request.Anio}. No se permite duplicar un año lectivo.");
            }

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
