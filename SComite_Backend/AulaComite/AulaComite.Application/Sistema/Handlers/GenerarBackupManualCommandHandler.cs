using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Sistema.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Sistema.Handlers
{
    public class GenerarBackupManualCommandHandler : IRequestHandler<GenerarBackupManualCommand, byte[]>
    {
        private readonly ISistemaRepository _repository;

        public GenerarBackupManualCommandHandler(ISistemaRepository repository)
        {
            _repository = repository;
        }

        public async Task<byte[]> Handle(GenerarBackupManualCommand request, CancellationToken cancellationToken)
        {
            // Genera script SQL / Backup en memoria o lectura de archivo de respaldo
            return await _repository.GenerarBackupScriptSqlAsync();
        }
    }
}
