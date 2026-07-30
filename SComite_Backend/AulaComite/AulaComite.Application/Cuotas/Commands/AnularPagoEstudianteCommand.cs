using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Cuotas.Commands
{
    public record AnularPagoEstudianteCommand(int CuotaDetalleId) : IRequest<bool>;
}
