using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Cuotas.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class GenerarCuotasMensualesCommandHandler : IRequestHandler<GenerarCuotasMensualesCommand, bool>
    {
        private readonly ICuotaRepository _cuotaRepository;
        private readonly IAulaRepository _aulaRepository;
        private readonly ILogRepository _logRepository;

        public GenerarCuotasMensualesCommandHandler(
            ICuotaRepository cuotaRepository,
            IAulaRepository aulaRepository,
            ILogRepository logRepository)
        {
            _cuotaRepository = cuotaRepository;
            _aulaRepository = aulaRepository;
            _logRepository = logRepository;
        }

        public async Task<bool> Handle(GenerarCuotasMensualesCommand request, CancellationToken cancellationToken)
        {
            await _cuotaRepository.GenerarProgramacionMensualAsync(
                request.AulaId,
                request.ConceptoBase,
                request.MontoMensual,
                request.MesInicio,
                request.DiaVencimiento,
                request.AnioLectivo
            );

            var aula = await _aulaRepository.ObtenerPorIdAsync(request.AulaId);
            string aulaDisplay = aula != null ? $"{aula.Nivel} - {aula.Grado}° \"{aula.Seccion}\"" : $"Aula #{request.AulaId}";

            await _logRepository.RegistrarAsync(
                nivel: "INFO",
                modulo: "TESORERIA",
                accion: "PROGRAMAR_CUOTAS_MENSUALES",
                mensaje: $"Se generó la programación mensual de '{request.ConceptoBase.ToUpper()}' (S/. {request.MontoMensual:F2}/mes, vence el día {request.DiaVencimiento}) para el Aula {aulaDisplay} en el Periodo {request.AnioLectivo}."
            );

            return true;
        }
    }
}
