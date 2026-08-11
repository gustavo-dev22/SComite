using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Dto;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Logs.Queries;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Logs.Handlers
{
    public class GetLogsQueryHandler : IRequestHandler<GetLogsQuery, PagedResultDto<LogSistema>>
    {
        private readonly ILogRepository _repository;

        public GetLogsQueryHandler(ILogRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResultDto<LogSistema>> Handle(GetLogsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerFiltradosAsync(
                request.FechaInicio,
                request.FechaFin,
                request.Nivel,
                request.Modulo,
                request.Busqueda,
                request.Pagina,
                request.TamanoPagina
            );
        }
    }
}
