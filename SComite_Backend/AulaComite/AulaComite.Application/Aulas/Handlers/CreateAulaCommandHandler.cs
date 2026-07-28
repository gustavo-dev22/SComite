using AulaComite.Application.Aulas.Commands;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Aulas.Handlers
{
    public class CreateAulaCommandHandler : IRequestHandler<CreateAulaCommand, int>
    {
        private readonly IAulaRepository _aulaRepository;
        private readonly ILogger<CreateAulaCommandHandler> _logger;

        public CreateAulaCommandHandler(IAulaRepository aulaRepository, ILogger<CreateAulaCommandHandler> logger)
        {
            _aulaRepository = aulaRepository;
            _logger = logger;
        }

        public async Task<int> Handle(CreateAulaCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creando nueva aula para el periodo {PeriodoId}: {Nivel} - {Grado} {Seccion}",
            request.PeriodoId, request.Nivel, request.Grado, request.Seccion);

            var nuevaAula = new Aula
            {
                PeriodoId = request.PeriodoId,
                Nivel = request.Nivel.ToUpper(),
                Grado = request.Grado.ToUpper(),
                Seccion = request.Seccion.ToUpper()
            };

            var idGenerado = await _aulaRepository.CrearAulaAsync(nuevaAula);

            _logger.LogInformation("Aula creada exitosamente con ID asignado {AulaId}", idGenerado);

            return idGenerado;
        }
    }
}
