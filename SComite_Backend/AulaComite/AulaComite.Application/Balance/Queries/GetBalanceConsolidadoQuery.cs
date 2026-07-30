using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Balance.Dtos;
using MediatR;

namespace AulaComite.Application.Balance.Queries
{
    public record GetBalanceConsolidadoQuery(int AulaId, int AnioLectivo, int? Mes) : IRequest<BalanceGeneralDTO>;
}
