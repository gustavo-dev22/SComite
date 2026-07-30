using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Gastos.Queries
{
    public record GetResumenCajaQuery(int AulaId) : IRequest<ResumenCajaAula>;
}
