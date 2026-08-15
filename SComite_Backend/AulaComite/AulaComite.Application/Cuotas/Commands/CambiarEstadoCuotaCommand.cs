using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Cuotas.Commands
{
    public record CambiarEstadoCuotaCommand(
        int CuotaId,
        string NuevoEstado // "CERRADA" o "EN COBRO"
    ) : IRequest<bool>;
}
