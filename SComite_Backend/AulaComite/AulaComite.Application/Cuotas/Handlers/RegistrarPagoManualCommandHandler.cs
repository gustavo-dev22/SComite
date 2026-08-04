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
        private readonly IDbConnectionFactory _connectionFactory;

        public RegistrarPagoManualCommandHandler(ICuotaRepository cuotaRepository, ILogRepository logRepository, IDbConnectionFactory connectionFactory)
        {
            _cuotaRepository = cuotaRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> Handle(RegistrarPagoManualCommand request, CancellationToken cancellationToken)
        {
            await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                await _cuotaRepository.RegistrarPagoManualAsync(request.CuotaDetalleId, request.MontoAbonado, request.FormaPago, transaction);

                await _logRepository.RegistrarAsync(
                    nivel: "INFO",
                    modulo: "TESORERIA",
                    accion: "REGISTRAR_PAGO_MANUAL",
                    mensaje: $"Se registró un pago manual de S/. {request.MontoAbonado:F2} ({request.FormaPago}) para la cuota detalle #{request.CuotaDetalleId}.",
                    transaction: transaction
                );
            });

            return true;
        }
    }
}
