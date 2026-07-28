using AulaComite.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Aulas.Queries
{
    public record GetAulasQuery(int? PeriodoId) : IRequest<IEnumerable<Aula>>;
}
