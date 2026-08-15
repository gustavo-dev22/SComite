using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Dto;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Logs.Queries;
using AulaComite.Domain.Entities;
using FluentValidation;
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
            // 🛡️ T2.1: Validación de paginación segura y rangos de fecha coherentes (→ 400).
            if (request.Pagina < 1)
                throw new ValidationException("El número de página debe ser mayor o igual a 1.");

            if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
                throw new ValidationException("El tamaño de página debe estar entre 1 y 100.");

            if (request.FechaInicio.HasValue && request.FechaFin.HasValue && request.FechaInicio > request.FechaFin)
                throw new ValidationException("La fecha de inicio no puede ser posterior a la fecha de fin.");

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
