using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Cuotas.Commands
{
    public record CreateCuotaCommand(
        int AulaId,
        string Concepto,
        decimal MontoIndividual,
        DateTime FechaVencimiento,
        string? Observacion
    ) : IRequest<int>;
}
