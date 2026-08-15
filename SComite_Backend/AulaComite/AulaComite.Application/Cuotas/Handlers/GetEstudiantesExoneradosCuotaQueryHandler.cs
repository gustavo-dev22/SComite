using AulaComite.Application.Common.Security;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Cuotas.Dtos;
using AulaComite.Application.Cuotas.Queries;
using MediatR;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class GetEstudiantesExoneradosCuotaQueryHandler : IRequestHandler<GetEstudiantesExoneradosCuotaQuery, List<EstudianteExoneradoCuotaDto>>
    {
        private readonly ICuotaRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetEstudiantesExoneradosCuotaQueryHandler(
            ICuotaRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<List<EstudianteExoneradoCuotaDto>> Handle(GetEstudiantesExoneradosCuotaQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: se resuelve el Aula de la cuota y se valida que el usuario pertenezca a ella.
            var aulaId = await _repository.ObtenerAulaIdPorCuotaAsync(request.CuotaId);
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, aulaId);

            var result = await _repository.ObtenerEstudiantesExoneradosAsync(request.CuotaId);

            return result.Select(e => new EstudianteExoneradoCuotaDto
            {
                CuotaDetalleId = e.CuotaDetalleId,
                EstudianteId = e.EstudianteId,
                TipoDocumento = e.TipoDocumento,
                NumeroDocumento = PiiMasker.EnmascararDocumento(e.NumeroDocumento),
                NombreEstudiante = e.NombreEstudiante,
                NombreApoderado = e.NombreApoderado,
                TelefonoApoderado = e.TelefonoApoderado,
                MontoAsignado = e.MontoAsignado,
                MotivoExoneracion = e.MotivoExoneracion,
                FechaExoneracion = e.FechaExoneracion
            }).ToList();
        }
    }
}