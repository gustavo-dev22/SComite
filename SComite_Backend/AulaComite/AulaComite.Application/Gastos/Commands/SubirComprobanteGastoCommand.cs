using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MediatR;

namespace AulaComite.Application.Gastos.Commands
{
    public record SubirComprobanteGastoCommand(Stream ContenidoArchivo, string NombreArchivo) : IRequest<string>;
}
