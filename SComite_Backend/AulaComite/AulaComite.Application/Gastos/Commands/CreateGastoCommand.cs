using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Gastos.Commands
{
    public record CreateGastoCommand(
        int AulaId,
        string Concepto,
        string Categoria,
        decimal Monto,
        DateTime FechaGasto,
        string TipoComprobante,
        string? NumeroComprobante,
        string? Proveedor,
        string? Observacion
    ) : IRequest<int>;
}
