using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Sistema.Commands;
using MediatR;

namespace AulaComite.Application.Sistema.Handlers
{
    public class ResetBaseDeDatosCommandHandler : IRequestHandler<ResetBaseDeDatosCommand, bool>
    {
        private readonly ISistemaRepository _repository;

        public ResetBaseDeDatosCommandHandler(ISistemaRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(ResetBaseDeDatosCommand request, CancellationToken cancellationToken)
        {
            // Validación de seguridad adicional en el servidor
            if (request.ConfirmacionTexto != "ELIMINAR TODO")
            {
                throw new InvalidOperationException("El texto de confirmación es incorrecto.");
            }

            return await _repository.ResetBaseDeDatosAsync();
        }
    }
}
