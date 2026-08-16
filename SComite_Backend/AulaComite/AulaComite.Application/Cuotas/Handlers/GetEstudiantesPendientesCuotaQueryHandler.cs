using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Cuotas.Dtos;
using AulaComite.Application.Cuotas.Queries;
using MediatR;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class GetEstudiantesPendientesCuotaQueryHandler
    : IRequestHandler<GetEstudiantesPendientesCuotaQuery, List<EstudiantePendienteCuotaDto>>
    {
        private readonly ICuotaRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GetEstudiantesPendientesCuotaQueryHandler(
            ICuotaRepository repository,
            IComiteRepository comiteRepository,
            IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<List<EstudiantePendienteCuotaDto>> Handle(
            GetEstudiantesPendientesCuotaQuery request,
            CancellationToken cancellationToken)
        {
            // 🛡️ IDOR mitigación: se resuelve el Aula de la cuota y se valida que el usuario pertenezca a ella.
            var aulaId = await _repository.ObtenerAulaIdPorCuotaAsync(request.CuotaId);
            if (!aulaId.HasValue)
                throw new KeyNotFoundException("No se encontró la cuota especificada.");

            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, aulaId);

            var result = await _repository.ObtenerEstudiantesPendientesAsync(request.CuotaId);

            // 🛡️ M7: En el listado se enmascara el documento del apoderado. El teléfono
            // se expone completo porque el comité/tesorería lo usa para contactar por
            // WhatsApp a los apoderados morosos.
            return result.Select(e => new EstudiantePendienteCuotaDto
            {
                CuotaDetalleId = e.CuotaDetalleId,
                EstudianteId = e.EstudianteId,
                TipoDocumento = e.TipoDocumento,
                NumeroDocumento = PiiMasker.EnmascararDocumento(e.NumeroDocumento),
                NombreEstudiante = e.NombreEstudiante,
                NombreApoderado = e.NombreApoderado,
                TelefonoApoderado = e.TelefonoApoderado,
                MontoAsignado = e.MontoAsignado,
                MontoPagado = e.MontoPagado,
                MontoPendiente = e.MontoPendiente,
                EstadoPago = e.EstadoPago
            }).ToList();
        }
    }
}