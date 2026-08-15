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
        private readonly IDbConnectionFactory _connectionFactory;

        public CreatePeriodoCommandHandler(IPeriodoRepository repository, ILogRepository logRepository, IDbConnectionFactory connectionFactory)
        {
            _repository = repository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<int> Handle(CreatePeriodoCommand request, CancellationToken cancellationToken)
        {
            if (await _repository.ExisteAnioAsync(request.Anio))
            {
                throw new FluentValidation.ValidationException($"Ya existe un Año Lectivo registrado para el año {request.Anio}. No se permite registrar el mismo año dos veces.");
            }

            var p = new PeriodoLectivo
            {
                Anio = request.Anio,
                FechaInicio = request.FechaInicio,
                FechaFin = request.FechaFin,
                EsActivo = request.EsActivo
            };

            int id = await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                return await _repository.CrearAsync(p, transaction);
            });

            // 🛡️ M13: El log se registra de forma independiente, fuera de la transacción de negocio.
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
