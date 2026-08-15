using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Cuotas.Commands;
using MediatR;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class CambiarEstadoCuotaCommandHandler : IRequestHandler<CambiarEstadoCuotaCommand, bool>
    {
        private const string EstadoCerrada = "CERRADA";
        private const string EstadoEnCobro = "EN COBRO";

        private readonly ICuotaRepository _cuotaRepository;
        private readonly IComiteRepository _comiteRepository;
        private readonly ILogRepository _logRepository;
        private readonly IUserContextService _userContextService;

        public CambiarEstadoCuotaCommandHandler(
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

        public async Task<bool> Handle(CambiarEstadoCuotaCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Solo se permite transitar entre CERRADA y EN COBRO; cualquier otro
            // valor (o nulo) se rechaza para no corromper el estado contable de la cuota.
            var nuevoEstado = request.NuevoEstado?.Trim().ToUpperInvariant();
            if (nuevoEstado != EstadoCerrada && nuevoEstado != EstadoEnCobro)
                return false;

            // 🛡️ Validar pertenencia: la cuota debe pertenecer a un Aula asignada al usuario.
            var aulaId = await _cuotaRepository.ObtenerAulaIdPorCuotaAsync(request.CuotaId);
            if (!aulaId.HasValue) return false;

            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, aulaId);

            await _cuotaRepository.CambiarEstadoCuotaAsync(request.CuotaId, nuevoEstado);

            // 🛡️ M13: El log se registra de forma independiente, fuera de la transacción de negocio.
            await _logRepository.RegistrarAsync(
                nivel: "WARN",
                modulo: "TESORERIA",
                accion: nuevoEstado == EstadoCerrada ? "CERRAR_CUOTA" : "REABRIR_CUOTA",
                mensaje: nuevoEstado == EstadoCerrada
                    ? $"Se cerró y saneó la cuota #{request.CuotaId}."
                    : $"Se reabrió la cobranza de la cuota #{request.CuotaId}."
            );

            return true;
        }
    }
}