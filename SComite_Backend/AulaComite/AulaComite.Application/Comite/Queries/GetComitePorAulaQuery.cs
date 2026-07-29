using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Comite.Queries
{
    public record GetComitePorAulaQuery(int AulaId) : IRequest<IEnumerable<ComiteIntegrante>>;
}
