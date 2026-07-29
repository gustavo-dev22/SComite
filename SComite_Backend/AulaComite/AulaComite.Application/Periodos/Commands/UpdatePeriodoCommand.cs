using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Periodos.Commands
{
    public record UpdatePeriodoCommand(
        int Id,
        int Anio,
        DateTime FechaInicio,
        DateTime FechaFin,
        bool EsActivo
    ) : IRequest<bool>;
}
