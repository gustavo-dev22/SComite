using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Gastos.Commands
{
    public record DeleteGastoCommand(int GastoId) : IRequest<bool>;
}
