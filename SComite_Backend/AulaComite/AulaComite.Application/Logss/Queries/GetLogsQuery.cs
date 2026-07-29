using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Logss.Queries
{
    public record GetLogsQuery(
        DateTime? FechaInicio,
        DateTime? FechaFin,
        string? Nivel,
        string? Modulo,
        string? Busqueda,
        int Pagina = 1,
        int TamanoPagina = 20
    ) : IRequest<PagedResultDto<LogSistema>>;
}
