using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AulaComite.Application.Aulas.Dtos;
using AulaComite.Application.Aulas.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AulaComite.Application.Aulas.Handlers
{
    public class GetBalanceAulaQueryHandler : IRequestHandler<GetBalanceAulaQuery, BalanceAulaDto>
    {
        private readonly ITransparenciaRepository _transparenciaRepository;
        private readonly IApoderadoRepository _apoderadoRepository;
        private readonly IUserContextService _userContextService;

        public GetBalanceAulaQueryHandler(
            ITransparenciaRepository transparenciaRepository,
            IApoderadoRepository apoderadoRepository,
            IUserContextService userContextService)
        {
            _transparenciaRepository = transparenciaRepository;
            _apoderadoRepository = apoderadoRepository;
            _userContextService = userContextService;
        }

        public async Task<BalanceAulaDto> Handle(GetBalanceAulaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: el acceso global queda reservado para Administradores Globales.
            // El apoderado solo puede consultar el balance de los Aulas donde tiene un hijo
            // matriculado; en caso contrario se devuelve 403 Forbidden (UnauthorizedAccessException).
            if (!_userContextService.EsAdministradorGlobal())
            {
                var usuarioApoderado = _userContextService.ObtenerUsuarioActual();
                if (string.IsNullOrEmpty(usuarioApoderado) || usuarioApoderado == "Anónimo")
                {
                    throw new UnauthorizedAccessException("No se pudo identificar al usuario autenticado. Acceso denegado.");
                }

                var hijos = await _apoderadoRepository.ObtenerHijosApoderadoAsync(usuarioApoderado, request.Anio);
                if (!hijos.Any(h => h.AulaId == request.AulaId))
                {
                    throw new UnauthorizedAccessException("El aula solicitada no corresponde a los hijos del apoderado autenticado. Acceso denegado.");
                }
            }

            return await _transparenciaRepository.ObtenerBalancePorAulaAsync(request.AulaId, request.Anio);
        }
    }
}