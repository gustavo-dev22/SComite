using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Donaciones.Dtos;
using MediatR;

namespace AulaComite.Application.Donaciones.Queries
{
    public record GetDonacionesPorAulaQuery(
        int AulaId,
        int AnioLectivo,
        int? Mes = null
    ) : IRequest<IEnumerable<DonacionDTO>>;
}
