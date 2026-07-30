using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Cuotas.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class AnularPagoEstudianteCommandHandler : IRequestHandler<AnularPagoEstudianteCommand, bool>
    {
        private readonly ICuotaRepository _cuotaRepository;
        private readonly ILogRepository _logRepository;

        public AnularPagoEstudianteCommandHandler(ICuotaRepository cuotaRepository, ILogRepository logRepository)
        {
            _cuotaRepository = cuotaRepository;
            _logRepository = logRepository;
        }

        public async Task<bool> Handle(AnularPagoEstudianteCommand request, CancellationToken cancellationToken)
        {
            await _cuotaRepository.AnularPagoEstudianteAsync(request.CuotaDetalleId);

            await _logRepository.RegistrarAsync(
                nivel: "WARN",
                modulo: "TESORERIA",
                accion: "ANULAR_PAGO",
                mensaje: $"Se anuló el estado de pago de la cuota detalle #{request.CuotaDetalleId} devolviéndola a PENDIENTE."
            );

            return true;
        }
    }
}
