using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Periodos.Commands;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Periodos.Handlers
{
    public class CreatePeriodoCommandHandler : IRequestHandler<CreatePeriodoCommand, int>
    {
        private readonly IPeriodoRepository _repository;
        private readonly ILogRepository _logRepository;

        public CreatePeriodoCommandHandler(IPeriodoRepository repository, ILogRepository logRepository)
        {
            _repository = repository;
            _logRepository = logRepository;
        }

        public async Task<int> Handle(CreatePeriodoCommand request, CancellationToken cancellationToken)
        {
            var p = new PeriodoLectivo
            {
                Anio = request.Anio,
                FechaInicio = request.FechaInicio,
                FechaFin = request.FechaFin,
                EsActivo = request.EsActivo
            };

            int id = await _repository.CrearAsync(p);

            await _logRepository.RegistrarAsync(
                nivel: "INFO",
                modulo: "PERIODOS",
                accion: "CREAR_PERIODO",
                mensaje: $"Se creó el Año Lectivo {request.Anio} (Vigente: {request.EsActivo}) con rango de fechas {request.FechaInicio:dd/MM/yyyy} - {request.FechaFin:dd/MM/yyyy}."
            );

            return id;
        }
    }
}
