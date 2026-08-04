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
        private readonly IDbConnectionFactory _connectionFactory;

        public DeleteGastoCommandHandler(IGastoRepository repository, ILogRepository logRepository, IDbConnectionFactory connectionFactory)
        {
            _repository = repository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> Handle(DeleteGastoCommand request, CancellationToken cancellationToken)
        {
            await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                await _repository.EliminarAsync(request.GastoId, transaction);

                await _logRepository.RegistrarAsync(
                    nivel: "WARN",
                    modulo: "TESORERIA",
                    accion: "ELIMINAR_GASTO",
                    mensaje: $"Se eliminó el registro de gasto #{request.GastoId} de la caja del aula.",
                    transaction: transaction
                );
            });

            return true;
        }
    }
}
