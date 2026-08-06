using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
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
            return result.ToList();
        }
    }
}
