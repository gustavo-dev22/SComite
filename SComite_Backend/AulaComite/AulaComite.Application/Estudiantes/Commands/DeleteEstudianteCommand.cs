using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Estudiantes.Commands
{
    public record DeleteEstudianteCommand(int Id) : IRequest<bool>;
}
