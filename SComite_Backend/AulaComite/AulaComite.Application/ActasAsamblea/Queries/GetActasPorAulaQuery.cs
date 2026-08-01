using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Queries
{
    public record GetActasPorAulaQuery(int AulaId, int AnioLectivo) : IRequest<IEnumerable<ActaAsambleaComite>>;
}
