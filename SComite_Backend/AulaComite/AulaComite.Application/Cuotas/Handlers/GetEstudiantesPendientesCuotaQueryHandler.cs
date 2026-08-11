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

        public GetEstudiantesPendientesCuotaQueryHandler(ICuotaRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<EstudiantePendienteCuotaDto>> Handle(
            GetEstudiantesPendientesCuotaQuery request,
            CancellationToken cancellationToken)
        {
            var result = await _repository.ObtenerEstudiantesPendientesAsync(request.CuotaId);

            // 🛡️ M7: En el listado se enmascaran documento y teléfono del apoderado.
            return result.Select(e => new EstudiantePendienteCuotaDto
            {
                EstudianteId = e.EstudianteId,
                TipoDocumento = e.TipoDocumento,
                NumeroDocumento = PiiMasker.EnmascararDocumento(e.NumeroDocumento),
                NombreEstudiante = e.NombreEstudiante,
                NombreApoderado = e.NombreApoderado,
                TelefonoApoderado = PiiMasker.EnmascararTelefono(e.TelefonoApoderado),
                MontoAsignado = e.MontoAsignado,
                MontoPagado = e.MontoPagado,
                MontoPendiente = e.MontoPendiente,
                EstadoPago = e.EstadoPago
            }).ToList();
        }
    }
}
