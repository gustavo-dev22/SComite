using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Aulas.Dtos;
using MediatR;

namespace AulaComite.Application.Aulas.Queries
{
    public record GetBalanceAulaQuery(int AulaId, int Anio) : IRequest<BalanceAulaDto>;
}
