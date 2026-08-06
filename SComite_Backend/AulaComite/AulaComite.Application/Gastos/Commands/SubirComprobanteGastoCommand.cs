using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Gastos.Commands
{
    public record SubirComprobanteGastoCommand(byte[] ContenidoArchivo, string NombreArchivo) : IRequest<string>;
}
