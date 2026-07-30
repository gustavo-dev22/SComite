using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Balance.Dtos
{
    public record BalanceGeneralDTO(
        BalanceConsolidado Consolidado,
        IEnumerable<GastoCategoriaResumen> GastosPorCategoria,
        IEnumerable<GastoComiteDTO> GastosDetalle
    );
}
