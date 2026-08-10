using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Cuotas.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class AnularPagoEstudianteCommandHandler : IRequestHandler<AnularPagoEstudianteCommand, bool>
    {
        private readonly ICuotaRepository _cuotaRepository;
        private readonly IComiteRepository _comiteRepository;
        private readonly ILogRepository _logRepository;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUserContextService _userContextService;

        public AnularPagoEstudianteCommandHandler(ICuotaRepository cuotaRepository, IComiteRepository comiteRepository, ILogRepository logRepository, IDbConnectionFactory connectionFactory, IUserContextService userContextService)
        {
            _cuotaRepository = cuotaRepository;
            _comiteRepository = comiteRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(AnularPagoEstudianteCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: el detalle de cuota debe pertenecer a un Aula asignada al usuario.
            var aulaId = await _cuotaRepository.ObtenerAulaIdPorCuotaDetalleAsync(request.CuotaDetalleId);
            if (!aulaId.HasValue) return false;

            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, aulaId);

            await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                await _cuotaRepository.AnularPagoEstudianteAsync(request.CuotaDetalleId, transaction);
            });

            // 🛡️ M13: El log se registra de forma independiente, fuera de la transacción de negocio.
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
