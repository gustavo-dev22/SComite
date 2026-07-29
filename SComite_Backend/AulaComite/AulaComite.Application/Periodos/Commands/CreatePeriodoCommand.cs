using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Periodos.Commands
{
    public record CreatePeriodoCommand(
        int Anio,
        DateTime FechaInicio,
        DateTime FechaFin,
        bool EsActivo
    ) : IRequest<int>;
}
