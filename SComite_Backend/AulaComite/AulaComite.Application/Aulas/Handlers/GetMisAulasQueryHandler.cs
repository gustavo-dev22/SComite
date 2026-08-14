using AulaComite.Application.Aulas.Dtos;
using AulaComite.Application.Aulas.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Aulas.Handlers
{
    public class GetMisAulasQueryHandler : IRequestHandler<GetMisAulasQuery, IEnumerable<AulaDto>>
    {
        private readonly IAulaRepository _aulaRepository;
        private readonly IUserContextService _userContext;

        public GetMisAulasQueryHandler(IAulaRepository aulaRepository, IUserContextService userContext)
        {
            _aulaRepository = aulaRepository;
            _userContext = userContext;
        }

        public async Task<IEnumerable<AulaDto>> Handle(GetMisAulasQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ El Administrador / Administrador Global puede ver todas las aulas.
            // Cualquier otro usuario (Comité de Aula o Apoderado) solo ve las aulas
            // a las que pertenece: por su membresía en el comité o por los hijos
            // de los que es apoderado (puede tener hijos en varias aulas).
            IEnumerable<Aula> aulas = _userContext.EsAdministradorGlobal()
                ? await _aulaRepository.ObtenerTodasAsync(request.PeriodoId)
                : await _aulaRepository.ObtenerAulasPorUsuarioAsync(
                    request.PeriodoId,
                    _userContext.ObtenerUsuarioId(),
                    _userContext.ObtenerUsuarioActual());

            return aulas.Select(a => new AulaDto
            {
                Id = a.Id,
                PeriodoId = a.PeriodoId,
                Nivel = a.Nivel,
                Grado = a.Grado,
                Seccion = a.Seccion,
                NombreDisplay = a.NombreDisplay,
                Estado = a.Estado,
                AnioPeriodo = a.AnioPeriodo
            });
        }
    }
}