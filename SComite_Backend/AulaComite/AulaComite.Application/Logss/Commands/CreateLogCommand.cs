using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Logss.Commands
{
    public record CreateLogCommand(
        string Nivel,
        string Modulo,
        string Accion,
        string Mensaje,
        string? DetalleException = null
    ) : IRequest<bool>;
}
