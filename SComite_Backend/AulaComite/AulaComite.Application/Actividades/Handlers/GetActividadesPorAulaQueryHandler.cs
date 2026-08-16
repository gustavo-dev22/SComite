using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Actividades.Dtos;
using AulaComite.Application.Actividades.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Actividades.Handlers
{
    /// <summary>
    /// 🚀 T3.5: Listado de actividades por aula. Soporte volumétrico actual:
    /// &lt;100 registros por aula (se devuelve IEnumerable sin paginar). El DTO queda
    /// preparado para migrar a una paginación futura (PagedResultDto&lt;T&gt;).
    /// </summary>
    public class GetActividadesPorAulaQueryHandler : IRequestHandler<GetActividadesPorAulaQuery, IEnumerable<ActividadComiteDto>>
    {
        private readonly IActividadRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetActividadesPorAulaQueryHandler(
            IActividadRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<IEnumerable<ActividadComiteDto>> Handle(GetActividadesPorAulaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el usuario debe pertenecer al Aula consultada (o ser Administrador Global).
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            return await _repository.ObtenerPorAulaAsync(request.AulaId, request.AnioLectivo);
        }
    }
}