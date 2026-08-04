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
        private readonly IDbConnectionFactory _connectionFactory;

        public AnularPagoEstudianteCommandHandler(ICuotaRepository cuotaRepository, ILogRepository logRepository, IDbConnectionFactory connectionFactory)
        {
            _cuotaRepository = cuotaRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> Handle(AnularPagoEstudianteCommand request, CancellationToken cancellationToken)
        {
            await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                await _cuotaRepository.AnularPagoEstudianteAsync(request.CuotaDetalleId, transaction);

                await _logRepository.RegistrarAsync(
                    nivel: "WARN",
                    modulo: "TESORERIA",
                    accion: "ANULAR_PAGO",
                    mensaje: $"Se anuló el estado de pago de la cuota detalle #{request.CuotaDetalleId} devolviéndola a PENDIENTE.",
                    transaction: transaction
                );
            });

            return true;
        }
    }
}
