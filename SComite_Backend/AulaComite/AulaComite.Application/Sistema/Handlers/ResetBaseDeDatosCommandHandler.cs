using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Sistema.Commands;
using MediatR;

namespace AulaComite.Application.Sistema.Handlers
{
    public class ResetBaseDeDatosCommandHandler : IRequestHandler<ResetBaseDeDatosCommand, ResetBaseDeDatosResult>
    {
        private readonly ISistemaRepository _repository;
        private readonly IUserContextService _userContextService;
        private readonly ILogRepository _logRepository;

        public ResetBaseDeDatosCommandHandler(
            ISistemaRepository repository,
            IUserContextService userContextService,
            ILogRepository logRepository)
        {
            _repository = repository;
            _userContextService = userContextService;
            _logRepository = logRepository;
        }

        public async Task<ResetBaseDeDatosResult> Handle(ResetBaseDeDatosCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Solo un Administrador Global (validado desde el Token JWT) puede resetear la BD.
            if (!_userContextService.EsAdministradorGlobal())
            {
                return new ResetBaseDeDatosResult(false, "Solo un Administrador Global puede ejecutar el reseteo de la base de datos.", EsErrorDeAutorizacion: true);
            }

            // Validación de seguridad adicional en el servidor
            if (request.ConfirmacionTexto != "ELIMINAR TODO")
            {
                return new ResetBaseDeDatosResult(false, "El texto de confirmación es incorrecto.");
            }

            // 🛡️ T2.3: Auditoría explícita ANTES de la purga. El log se registra de forma
            // independiente, fuera de cualquier transacción de negocio, y queda incluido
            // en el script de respaldo pre-purga que se genera antes de limpiar la BD.
            await _logRepository.RegistrarAsync(
                nivel: "WARN",
                modulo: "SISTEMA",
                accion: "RESET_DATABASE",
                mensaje: $"Reseteo total de la base de datos solicitado por {_userContextService.ObtenerUsuarioActual()}."
            );

            await _repository.ResetBaseDeDatosAsync();
            return new ResetBaseDeDatosResult(true, "Se ha generado el backup pre-purga y la base de datos se ha limpiado por completo.");
        }
    }
}
