using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Aulas.Commands
{
    public record UpdateAulaCommand(int Id, int PeriodoId, string Nivel, string Grado, string Seccion) : IRequest<bool>;
}
