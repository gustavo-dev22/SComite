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

        public CreateLogCommandHandler(ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task<bool> Handle(CreateLogCommand request, CancellationToken cancellationToken)
        {
            await _logRepository.RegistrarAsync(
                nivel: request.Nivel,
                modulo: request.Modulo,
                accion: request.Accion,
                mensaje: request.Mensaje,
                usuario: request.Usuario,
                ip: request.IP,
                exception: request.DetalleException
            );

            return true;
        }
    }
}
