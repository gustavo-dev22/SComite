using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Cuotas.Dtos;
using AulaComite.Application.Cuotas.Queries;
using MediatR;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class GetCuotasPorAulaQueryHandler : IRequestHandler<GetCuotasPorAulaQuery, IEnumerable<CuotaDto>>
    {
        private readonly ICuotaRepository _repository;

        public GetCuotasPorAulaQueryHandler(ICuotaRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CuotaDto>> Handle(GetCuotasPorAulaQuery request, CancellationToken cancellationToken)
        {
            var cuotas = await _repository.ObtenerPorAulaAsync(request.AulaId);

            return cuotas.Select(c => new CuotaDto
            {
                Id = c.Id,
                AulaId = c.AulaId,
                ActividadId = c.ActividadId,
                Concepto = c.Concepto,
                MontoIndividual = c.MontoIndividual,
                FechaVencimiento = c.FechaVencimiento,
                Estado = c.Estado,
                Observacion = c.Observacion,
                FechaCreacion = c.FechaCreacion,
                TipoCuota = c.TipoCuota,
                MesCorrespondiente = c.MesCorrespondiente,
                TotalEstudiantesAsignados = c.TotalEstudiantesAsignados,
                TotalMontoEsperado = c.TotalMontoEsperado,
                TotalMontoRecaudado = c.TotalMontoRecaudado,
                EstudiantesAlDia = c.EstudiantesAlDia,
                EstudiantesPendientes = c.EstudiantesPendientes
            });
        }
    }
}