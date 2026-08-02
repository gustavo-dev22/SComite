using AulaComite.Application.Apoderado.Dtos;
using AulaComite.Application.Apoderado.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Handlers
{
    public class GetCuotasPendientesApoderadoQueryHandler : IRequestHandler<GetCuotasPendientesApoderadoQuery, ResumenPagosApoderadoDto>
    {
        private readonly IApoderadoRepository _repository;

        public GetCuotasPendientesApoderadoQueryHandler(IApoderadoRepository repository) => _repository = repository;

        public async Task<ResumenPagosApoderadoDto> Handle(GetCuotasPendientesApoderadoQuery request, CancellationToken cancellationToken)
        {
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
