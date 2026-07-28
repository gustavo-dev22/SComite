using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Aulas.Commands
{
    public record CreateAulaCommand(int PeriodoId, string Nivel, string Grado, string Seccion) : IRequest<int>;
}
