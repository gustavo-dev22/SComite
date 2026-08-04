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
        private readonly IDbConnectionFactory _connectionFactory;

        public GenerarCuotasMensualesCommandHandler(
            ICuotaRepository cuotaRepository,
            IAulaRepository aulaRepository,
            ILogRepository logRepository,
            IDbConnectionFactory connectionFactory)
        {
            _cuotaRepository = cuotaRepository;
            _aulaRepository = aulaRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> Handle(GenerarCuotasMensualesCommand request, CancellationToken cancellationToken)
        {
            var aula = await _aulaRepository.ObtenerPorIdAsync(request.AulaId);
            string aulaDisplay = aula != null ? $"{aula.Nivel} - {aula.Grado}° \"{aula.Seccion}\"" : $"Aula #{request.AulaId}";

            await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                await _cuotaRepository.GenerarProgramacionMensualAsync(
                    request.AulaId,
                    request.ConceptoBase,
                    request.MontoMensual,
                    request.MesInicio,
                    request.DiaVencimiento,
                    request.AnioLectivo,
                    transaction
                );

                await _logRepository.RegistrarAsync(
                    nivel: "INFO",
                    modulo: "TESORERIA",
                    accion: "PROGRAMAR_CUOTAS_MENSUALES",
                    mensaje: $"Se generó la programación mensual de '{request.ConceptoBase.ToUpper()}' (S/. {request.MontoMensual:F2}/mes, vence el día {request.DiaVencimiento}) para el Aula {aulaDisplay} en el Periodo {request.AnioLectivo}.",
                    transaction: transaction
                );
            });

            return true;
        }
    }
}
