using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace AulaComite.Application.Gastos.Commands
{
    public record UpdateGastoCommand(
        int Id,
        int AulaId,
        string Concepto,
        string Categoria,
        decimal Monto,
        DateTime FechaGasto,
        string TipoComprobante,
        string? NumeroComprobante,
        string? Proveedor,
        string? Observacion,
        string? UrlComprobante
    ) : IRequest<bool>;
}
