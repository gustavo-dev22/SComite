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

        public ResetBaseDeDatosCommandHandler(ISistemaRepository repository, IUserContextService userContextService)
        {
            _repository = repository;
            _userContextService = userContextService;
        }

        public async Task<ResetBaseDeDatosResult> Handle(ResetBaseDeDatosCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Protección reforzada: la operación de reseteo queda DESHABILITADA en entornos
            // de producción (defensa en profundidad, además del guard del controlador).
            var ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (ambiente.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                return new ResetBaseDeDatosResult(false, "La operación de reseteo de la base de datos está deshabilitada en producción.");
            }

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

            await _repository.ResetBaseDeDatosAsync();
            return new ResetBaseDeDatosResult(true, "Se ha generado el backup pre-purga y la base de datos se ha limpiado por completo.");
        }
    }
}
