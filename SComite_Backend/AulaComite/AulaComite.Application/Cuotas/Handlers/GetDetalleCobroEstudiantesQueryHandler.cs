using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Cuotas.Dtos;
using AulaComite.Application.Cuotas.Queries;
using MediatR;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class GetDetalleCobroEstudiantesQueryHandler : IRequestHandler<GetDetalleCobroEstudiantesQuery, IEnumerable<CuotaEstudianteCobroDto>>
    {
        private readonly ICuotaRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetDetalleCobroEstudiantesQueryHandler(
            ICuotaRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<IEnumerable<CuotaEstudianteCobroDto>> Handle(GetDetalleCobroEstudiantesQuery request, CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: se resuelve el Aula de la cuota y se valida que el usuario pertenezca a ella.
            var aulaId = await _repository.ObtenerAulaIdPorCuotaAsync(request.CuotaId);
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, aulaId);

            var cobros = await _repository.ObtenerDetalleCobroEstudiantesAsync(request.CuotaId);

            // 🛡️ M7: Se enmascara el documento del estudiante. El teléfono del apoderado se
            // expone completo porque la tesorería lo usa para contactar por WhatsApp.
            return cobros.Select(c => new CuotaEstudianteCobroDto
            {
                CuotaDetalleId = c.CuotaDetalleId,
                CuotaId = c.CuotaId,
                EstudianteId = c.EstudianteId,
                EstudianteNombreCompleto = c.EstudianteNombreCompleto,
                EstudianteDocumento = PiiMasker.EnmascararDocumento(c.EstudianteDocumento),
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