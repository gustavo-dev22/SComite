using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Cuotas.Queries
{
    public record GetDetalleCobroEstudiantesQuery(int CuotaId) : IRequest<IEnumerable<CuotaEstudianteCobro>>;
}
