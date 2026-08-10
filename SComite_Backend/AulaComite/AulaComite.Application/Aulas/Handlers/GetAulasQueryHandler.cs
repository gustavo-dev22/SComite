using AulaComite.Application.Aulas.Dtos;
using AulaComite.Application.Aulas.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Aulas.Handlers
{
    public class GetAulasQueryHandler : IRequestHandler<GetAulasQuery, IEnumerable<AulaDto>>
    {
        private readonly IAulaRepository _aulaRepository;

        public GetAulasQueryHandler(IAulaRepository aulaRepository)
        {
            _aulaRepository = aulaRepository;
        }

        public async Task<IEnumerable<AulaDto>> Handle(GetAulasQuery request, CancellationToken cancellationToken)
        {
            var aulas = await _aulaRepository.ObtenerTodasAsync(request.PeriodoId);

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