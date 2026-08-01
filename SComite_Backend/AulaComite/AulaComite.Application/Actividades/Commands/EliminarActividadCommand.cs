using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Actividades.Commands
{
    public record EliminarActividadCommand(int Id, int AulaId) : IRequest<bool>;
}
