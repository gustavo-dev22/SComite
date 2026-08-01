using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Donaciones.Dtos
{
    public record DonacionDTO(
        int Id,
        int AulaId,
        string Donante,
        decimal Monto,
        DateTime FechaDonacion,
        string Concepto,
        string? Observacion,
        DateTime FechaRegistro
    );
}
