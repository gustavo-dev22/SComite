using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Balance.Dtos
{
    public record GastoComiteDTO(
        int Id,
        DateTime FechaGasto,
        string Concepto,
        string Categoria,
        decimal Monto,
        string TipoComprobante,
        string? NumeroComprobante,
        string? Proveedor
    );
}
