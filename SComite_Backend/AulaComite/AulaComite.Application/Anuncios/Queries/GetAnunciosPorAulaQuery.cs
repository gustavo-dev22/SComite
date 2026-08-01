using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Anuncios.Queries
{
    public record GetAnunciosPorAulaQuery(int AulaId, int AnioLectivo) : IRequest<IEnumerable<AnuncioComite>>;
}
