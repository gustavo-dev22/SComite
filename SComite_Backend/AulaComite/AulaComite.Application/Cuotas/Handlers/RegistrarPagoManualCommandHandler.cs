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
    public class RegistrarPagoManualCommandHandler : IRequestHandler<RegistrarPagoManualCommand, bool>
    {
        private readonly ICuotaRepository _cuotaRepository;
        private readonly IComiteRepository _comiteRepository;
        private readonly ILogRepository _logRepository;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUserContextService _userContextService;

        private const string EstadoCuotaCerrada = "CERRADA";

        public RegistrarPagoManualCommandHandler(ICuotaRepository cuotaRepository, IComiteRepository comiteRepository, ILogRepository logRepository, IDbConnectionFactory connectionFactory, IUserContextService userContextService)
        {
            _cuotaRepository = cuotaRepository;
            _comiteRepository = comiteRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(RegistrarPagoManualCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: el detalle de cuota debe pertenecer a un Aula asignada al usuario.
            var aulaId = await _cuotaRepository.ObtenerAulaIdPorCuotaDetalleAsync(request.CuotaDetalleId);
            if (!aulaId.HasValue)
                throw new KeyNotFoundException("No se encontró el detalle de cuota del estudiante.");

            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, aulaId);

            // 🛡️ Una cuota cerrada/saneada no admite registros de pago.
            var estadoCuota = await _cuotaRepository.ObtenerEstadoCuotaPorCuotaDetalleAsync(request.CuotaDetalleId);
            if (string.Equals(estadoCuota, EstadoCuotaCerrada, StringComparison.OrdinalIgnoreCase))
                return false;

            var detalle = await _cuotaRepository.ObtenerDetalleCobroInfoAsync(request.CuotaDetalleId);

            await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                await _cuotaRepository.RegistrarPagoManualAsync(
                    request.CuotaDetalleId,
                    request.MontoAbonado,
                    request.FormaPago,
                    _userContextService.ObtenerUsuarioActual(),
                    transaction);
            });

            string conceptoMostrar = detalle != null && !string.IsNullOrWhiteSpace(detalle.Concepto)
                ? detalle.Concepto
                : $"Cuota #{request.CuotaDetalleId}";
            string estudianteMostrar = detalle != null && !string.IsNullOrWhiteSpace(detalle.EstudianteNombreCompleto)
                ? detalle.EstudianteNombreCompleto
                : $"Detalle #{request.CuotaDetalleId}";

            // 🛡️ M13: El log se registra de forma independiente, fuera de la transacción de negocio.
            await _logRepository.RegistrarAsync(
                nivel: "INFO",
                modulo: "TESORERIA",
                accion: "REGISTRAR_PAGO_MANUAL",
                mensaje: $"Se registró un pago manual de S/. {request.MontoAbonado:F2} ({request.FormaPago}) para el estudiante {estudianteMostrar} en la cuota '{conceptoMostrar}'."
            );

            return true;
        }
    }
}
