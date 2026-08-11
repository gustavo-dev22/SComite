using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Gastos.Dtos;

namespace AulaComite.Application.Balance.Dtos
{
    public record BalanceGeneralDto(
        BalanceConsolidadoDto Consolidado,
        IEnumerable<GastoCategoriaResumenDto> GastosPorCategoria,
        IEnumerable<GastoComiteDto> GastosDetalle
    );
}