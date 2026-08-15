using AulaComite.Application.Apoderado.Dtos;
using AulaComite.Application.Apoderado.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Apoderado.Handlers
{
    public class GetCronogramaEventosApoderadoQueryHandler
    : IRequestHandler<GetCronogramaEventosApoderadoQuery, List<EventoCronogramaApoderadoDto>>
    {
        private readonly IApoderadoRepository _repository;
        private readonly IUserContextService _userContextService;

        public GetCronogramaEventosApoderadoQueryHandler(IApoderadoRepository repository, IUserContextService userContextService)
        {
            _repository = repository;
            _userContextService = userContextService;
        }

        public async Task<List<EventoCronogramaApoderadoDto>> Handle(
            GetCronogramaEventosApoderadoQuery request,
            CancellationToken cancellationToken)
        {
            var esHijo = await ApoderadoAccessValidator.EsEstudianteDelApoderadoAsync(
                _repository, _userContextService, request.EstudianteId, request.AnioLectivo);

            if (!esHijo)
            {
                // 🛡️ T2.6: No se puede consultar un estudiante que no es hijo del apoderado
                // autenticado. El middleware convierte esto en 403 Forbidden.
                throw new UnauthorizedAccessException("El estudiante solicitado no pertenece al apoderado autenticado.");
            }

            var result = await _repository.ObtenerCronogramaEventosAsync(request.EstudianteId, request.AnioLectivo);
            return result.ToList();
        }
    }
}