using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Cuotas.Dtos;
using AulaComite.Application.Cuotas.Queries;
using MediatR;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class GetDetalleCobroEstudiantesQueryHandler : IRequestHandler<GetDetalleCobroEstudiantesQuery, IEnumerable<CuotaEstudianteCobroDto>>
    {
        private readonly ICuotaRepository _repository;

        public GetDetalleCobroEstudiantesQueryHandler(ICuotaRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CuotaEstudianteCobroDto>> Handle(GetDetalleCobroEstudiantesQuery request, CancellationToken cancellationToken)
        {
            var cobros = await _repository.ObtenerDetalleCobroEstudiantesAsync(request.CuotaId);

            return cobros.Select(c => new CuotaEstudianteCobroDto
            {
                CuotaDetalleId = c.CuotaDetalleId,
                CuotaId = c.CuotaId,
                EstudianteId = c.EstudianteId,
                EstudianteNombreCompleto = c.EstudianteNombreCompleto,
                EstudianteDocumento = c.EstudianteDocumento,
                NombreApoderado = c.NombreApoderado,
                TelefonoApoderado = c.TelefonoApoderado,
                MontoAsignado = c.MontoAsignado,
                MontoPagado = c.MontoPagado,
                EstadoPago = c.EstadoPago,
                FechaUltimoPago = c.FechaUltimoPago
            });
        }
    }
}