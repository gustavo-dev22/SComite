using AulaComite.Application.Apoderado.Dtos;
using AulaComite.Application.Apoderado.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Apoderado.Handlers
{
    public class GetCuotasPendientesApoderadoQueryHandler : IRequestHandler<GetCuotasPendientesApoderadoQuery, ResumenPagosApoderadoDto>
    {
        private readonly IApoderadoRepository _repository;
        private readonly IUserContextService _userContextService;

        public GetCuotasPendientesApoderadoQueryHandler(IApoderadoRepository repository, IUserContextService userContextService)
        {
            _repository = repository;
            _userContextService = userContextService;
        }

        public async Task<ResumenPagosApoderadoDto> Handle(GetCuotasPendientesApoderadoQuery request, CancellationToken cancellationToken)
        {
            var esHijo = await ApoderadoAccessValidator.EsEstudianteDelApoderadoAsync(
                _repository, _userContextService, request.EstudianteId, request.AnioLectivo);

            if (!esHijo)
            {
                return new ResumenPagosApoderadoDto
                {
                    EstudianteId = request.EstudianteId,
                    Cuotas = new List<CuotaApoderadoDto>()
                };
            }

            var cuotas = (await _repository.ObtenerCuotasPendientesAsync(request.EstudianteId, request.AnioLectivo)).ToList();

            return new ResumenPagosApoderadoDto
            {
                EstudianteId = request.EstudianteId,
                TotalPendiente = cuotas.Where(x => x.EstadoVisual != "PAGADO").Sum(x => x.MontoPendiente),
                TotalPagado = cuotas.Where(x => x.EstadoVisual == "PAGADO").Sum(x => x.MontoPagado),
                CantidadVencidas = cuotas.Count(x => x.EstadoVisual == "VENCIDO"),
                Cuotas = cuotas
            };
        }
    }
}