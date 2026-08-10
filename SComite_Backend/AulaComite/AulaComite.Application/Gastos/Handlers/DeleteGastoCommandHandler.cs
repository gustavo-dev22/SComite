using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Gastos.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Gastos.Handlers
{
    public class DeleteGastoCommandHandler : IRequestHandler<DeleteGastoCommand, bool>
    {
        private readonly IGastoRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogRepository _logRepository;
        private readonly IDbConnectionFactory _connectionFactory;

        public DeleteGastoCommandHandler(IGastoRepository repository, IComiteRepository comiteRepository, IUserContextService userContextService, IFileStorageService fileStorageService, ILogRepository logRepository, IDbConnectionFactory connectionFactory)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
            _fileStorageService = fileStorageService;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> Handle(DeleteGastoCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener los datos del gasto antes de eliminarlo
            var gasto = await _repository.ObtenerPorIdAsync(request.GastoId);
            if (gasto == null) return false;

            // 🛡️ Validar pertenencia: el gasto debe pertenecer a un Aula asignada al usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, gasto.AulaId);

            var urlComprobante = gasto.UrlComprobante;

            // 2. Eliminar el registro en base de datos dentro de una transacción atómica.
            await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                await _repository.EliminarAsync(request.GastoId, transaction);
            });

            // 🛡️ M13: El log de auditoría se registra de forma independiente, FUERA de la transacción.
            await _logRepository.RegistrarAsync(
                nivel: "WARN",
                modulo: "TESORERIA",
                accion: "ELIMINAR_GASTO",
                mensaje: $"Se eliminó el registro de gasto #{request.GastoId} por un monto de S/. {gasto.Monto:N2} de la caja del aula."
            );

            // 3. Una vez confirmada la eliminación en BD, borrar el comprobante del disco si existía
            if (!string.IsNullOrEmpty(urlComprobante))
            {
                _fileStorageService.EliminarComprobante(urlComprobante);
            }

            return true;
        }
    }
}
