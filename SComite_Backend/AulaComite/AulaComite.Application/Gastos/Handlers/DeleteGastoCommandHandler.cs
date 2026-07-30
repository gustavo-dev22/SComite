using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Gastos.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Gastos.Handlers
{
    public class DeleteGastoCommandHandler : IRequestHandler<DeleteGastoCommand, bool>
    {
        private readonly IGastoRepository _repository;
        private readonly ILogRepository _logRepository;

        public DeleteGastoCommandHandler(IGastoRepository repository, ILogRepository logRepository)
        {
            _repository = repository;
            _logRepository = logRepository;
        }

        public async Task<bool> Handle(DeleteGastoCommand request, CancellationToken cancellationToken)
        {
            await _repository.EliminarAsync(request.GastoId);

            await _logRepository.RegistrarAsync(
                nivel: "WARN",
                modulo: "TESORERIA",
                accion: "ELIMINAR_GASTO",
                mensaje: $"Se eliminó el registro de gasto #{request.GastoId} de la caja del aula."
            );

            return true;
        }
    }
}
