using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Sistema.Commands
{
    public record GenerarBackupManualCommand() : IRequest<byte[]>;
}
