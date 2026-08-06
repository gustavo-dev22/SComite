using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Gastos.Commands;
using MediatR;

namespace AulaComite.Application.Gastos.Handlers
{
    public class SubirComprobanteGastoCommandHandler : IRequestHandler<SubirComprobanteGastoCommand, string>
    {
        private readonly IFileStorageService _fileStorageService;

        public SubirComprobanteGastoCommandHandler(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        public async Task<string> Handle(SubirComprobanteGastoCommand request, CancellationToken cancellationToken)
        {
            return await _fileStorageService.GuardarComprobanteAsync(
                request.ContenidoArchivo,
                request.NombreArchivo,
                cancellationToken
            );
        }
    }
}
