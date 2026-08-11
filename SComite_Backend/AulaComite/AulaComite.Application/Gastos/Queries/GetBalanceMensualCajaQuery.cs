using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Gastos.Dtos;
using MediatR;

namespace AulaComite.Application.Gastos.Queries
{
    public record GetBalanceMensualCajaQuery(int AulaId, int AnioLectivo, int? Mes) : IRequest<ResumenCajaAulaDto>;
}
