using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Donaciones.Commands
{
    public record GuardarDonacionCommand(
        int Id,
        int AulaId,
        string Donante,
        decimal Monto,
        DateTime FechaDonacion,
        string Concepto,
        string? Observacion
    ) : IRequest<int>;
}
