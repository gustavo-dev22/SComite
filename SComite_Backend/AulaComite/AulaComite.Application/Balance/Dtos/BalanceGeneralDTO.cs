using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Balance.Dtos
{
    public record BalanceGeneralDTO(
        BalanceConsolidadoDto Consolidado,
        IEnumerable<GastoCategoriaResumenDto> GastosPorCategoria,
        IEnumerable<GastoComiteDTO> GastosDetalle
    );
}