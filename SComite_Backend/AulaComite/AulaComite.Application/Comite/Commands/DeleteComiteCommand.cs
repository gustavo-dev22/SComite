using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Comite.Commands
{
    public record DeleteComiteCommand(int Id) : IRequest<bool>;
}
