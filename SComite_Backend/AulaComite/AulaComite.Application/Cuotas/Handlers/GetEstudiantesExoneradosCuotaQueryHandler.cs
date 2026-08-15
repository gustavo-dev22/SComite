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

        public GetEstudiantesExoneradosCuotaQueryHandler(ICuotaRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<EstudianteExoneradoCuotaDto>> Handle(GetEstudiantesExoneradosCuotaQuery request, CancellationToken cancellationToken)
        {
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
