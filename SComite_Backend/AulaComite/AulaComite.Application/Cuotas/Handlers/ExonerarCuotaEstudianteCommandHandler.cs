using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Cuotas.Commands;
using MediatR;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class ExonerarCuotaEstudianteCommandHandler : IRequestHandler<ExonerarCuotaEstudianteCommand, bool>
    {
        private const string EstadoExonerado = "EXONERADO";
        private const string EstadoPendiente = "PENDIENTE";
        private const string EstadoCuotaCerrada = "CERRADA";

        private readonly ICuotaRepository _cuotaRepository;
        private readonly IComiteRepository _comiteRepository;
        private readonly ILogRepository _logRepository;
        private readonly IUserContextService _userContextService;

        public ExonerarCuotaEstudianteCommandHandler(
            ICuotaRepository cuotaRepository,
            IComiteRepository comiteRepository,
            ILogRepository logRepository,
            IUserContextService userContextService)
        {
            _cuotaRepository = cuotaRepository;
            _comiteRepository = comiteRepository;
            _logRepository = logRepository;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(ExonerarCuotaEstudianteCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Solo se permite transitar entre EXONERADO y PENDIENTE; cualquier otro
            // estado (o un valor nulo) se rechaza para no corromper la contabilidad.
            var nuevoEstado = request.NuevoEstado?.Trim().ToUpperInvariant();
            if (nuevoEstado != EstadoExonerado && nuevoEstado != EstadoPendiente)
                return false;

            // 🛡️ Validar pertenencia: el detalle de cuota debe pertenecer a un Aula asignada al usuario.
            var aulaId = await _cuotaRepository.ObtenerAulaIdPorCuotaDetalleAsync(request.CuotaDetalleId);
            if (!aulaId.HasValue)
                throw new KeyNotFoundException("No se encontró el detalle de cuota del estudiante.");

            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, aulaId);

            // 🛡️ Una cuota cerrada/saneada no admite exoneraciones ni reversiones.
            var estadoCuota = await _cuotaRepository.ObtenerEstadoCuotaPorCuotaDetalleAsync(request.CuotaDetalleId);
            if (string.Equals(estadoCuota, EstadoCuotaCerrada, StringComparison.OrdinalIgnoreCase))
                return false;

            var detalle = await _cuotaRepository.ObtenerDetalleCobroInfoAsync(request.CuotaDetalleId);

            await _cuotaRepository.CambiarEstadoExoneracionAsync(request.CuotaDetalleId, nuevoEstado, request.MotivoExoneracion);

            string conceptoMostrar = detalle != null && !string.IsNullOrWhiteSpace(detalle.Concepto)
                ? detalle.Concepto
                : $"Cuota #{request.CuotaDetalleId}";
            string estudianteMostrar = detalle != null && !string.IsNullOrWhiteSpace(detalle.EstudianteNombreCompleto)
                ? detalle.EstudianteNombreCompleto
                : $"Detalle #{request.CuotaDetalleId}";

            // 🛡️ M13: El log se registra de forma independiente, fuera de la transacción de negocio.
            await _logRepository.RegistrarAsync(
                nivel: nuevoEstado == EstadoExonerado ? "WARN" : "INFO",
                modulo: "TESORERIA",
                accion: nuevoEstado == EstadoExonerado ? "EXONERAR_CUOTA" : "REVERTIR_EXONERACION",
                mensaje: nuevoEstado == EstadoExonerado
                    ? $"Se exoneró la cuota '{conceptoMostrar}' del estudiante {estudianteMostrar}. Motivo: {request.MotivoExoneracion ?? "Sin especificar"}."
                    : $"Se revirtió la exoneración de la cuota '{conceptoMostrar}' del estudiante {estudianteMostrar}, volviendo a PENDIENTE."
            );

            return true;
        }
    }
}