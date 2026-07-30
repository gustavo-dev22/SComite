using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Cuotas.Commands
{
    public record RegistrarPagoManualCommand(
        int CuotaDetalleId,
        decimal MontoAbonado,
        string FormaPago
    ) : IRequest<bool>;
}
