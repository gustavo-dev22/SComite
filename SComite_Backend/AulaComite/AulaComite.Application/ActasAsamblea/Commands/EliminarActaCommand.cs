using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Commands
{
    public record EliminarActaCommand(int Id, int AulaId) : IRequest<bool>;
}
