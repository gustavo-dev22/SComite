using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Donaciones.Commands
{
    public record EliminarDonacionCommand(
        int Id,
        int AulaId
    ) : IRequest<bool>;
}
