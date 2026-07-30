using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Cuotas.Queries
{
    public record GetCuotasPorAulaQuery(int AulaId) : IRequest<IEnumerable<Cuota>>;
}
