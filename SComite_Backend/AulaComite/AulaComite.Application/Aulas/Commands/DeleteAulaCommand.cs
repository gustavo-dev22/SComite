using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Aulas.Commands
{
    public record DeleteAulaCommand(int Id) : IRequest<bool>;
}
