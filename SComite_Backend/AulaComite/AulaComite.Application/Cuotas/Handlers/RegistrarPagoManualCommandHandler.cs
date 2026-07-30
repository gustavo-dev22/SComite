using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Cuotas.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class RegistrarPagoManualCommandHandler : IRequestHandler<RegistrarPagoManualCommand, bool>
    {
        private readonly ICuotaRepository _cuotaRepository;
        private readonly ILogRepository _logRepository;

        public RegistrarPagoManualCommandHandler(ICuotaRepository cuotaRepository, ILogRepository logRepository)
        {
            _cuotaRepository = cuotaRepository;
            _logRepository = logRepository;
        }

        public async Task<bool> Handle(RegistrarPagoManualCommand request, CancellationToken cancellationToken)
        {
            await _cuotaRepository.RegistrarPagoManualAsync(request.CuotaDetalleId, request.MontoAbonado, request.FormaPago);

            await _logRepository.RegistrarAsync(
                nivel: "INFO",
                modulo: "TESORERIA",
                accion: "REGISTRAR_PAGO_MANUAL",
                mensaje: $"Se registró un pago manual de S/. {request.MontoAbonado:F2} ({request.FormaPago}) para la cuota detalle #{request.CuotaDetalleId}."
            );

            return true;
        }
    }
}
