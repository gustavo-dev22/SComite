using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Sistema.Commands
{
    public record ResetBaseDeDatosCommand(string ConfirmacionTexto) : IRequest<ResetBaseDeDatosResult>;
}
