using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Logss.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Logss.Handlers
{
    public class CreateLogCommandHandler : IRequestHandler<CreateLogCommand, bool>
    {
        private readonly ILogRepository _logRepository;
        private readonly IUserContextService _userContextService;

        public CreateLogCommandHandler(ILogRepository logRepository, IUserContextService userContextService)
        {
            _logRepository = logRepository;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(CreateLogCommand request, CancellationToken cancellationToken)
        {
            // Auditoría derivada exclusivamente del token JWT autenticado y de la petición HTTP,
            // nunca de datos enviados por el cliente en el cuerpo JSON.
            await _logRepository.RegistrarAsync(
                nivel: request.Nivel,
                modulo: request.Modulo,
                accion: request.Accion,
                mensaje: request.Mensaje,
                usuario: _userContextService.ObtenerUsuarioActual(),
                ip: _userContextService.ObtenerIpCliente(),
                exception: request.DetalleException
            );

            return true;
        }
    }
}
