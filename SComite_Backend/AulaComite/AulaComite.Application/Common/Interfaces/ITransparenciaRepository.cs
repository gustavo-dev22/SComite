using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Aulas.Dtos;

namespace AulaComite.Application.Common.Interfaces
{
    public interface ITransparenciaRepository
    {
        Task<BalanceAulaDto> ObtenerBalancePorAulaAsync(int aulaId, int anio);
    }
}
