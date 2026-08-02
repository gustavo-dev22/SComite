using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Auditoria.Dtos;
using AulaComite.Application.Auditoria.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Auditoria.Handlers
{
    public class GetResumenGeneralCajasQueryHandler : IRequestHandler<GetResumenGeneralCajasQuery, ResumenGeneralCajasConsolidadasDto>
    {
        private readonly IAuditoriaRepository _repository;

        public GetResumenGeneralCajasQueryHandler(IAuditoriaRepository repository) => _repository = repository;

        public async Task<ResumenGeneralCajasConsolidadasDto> Handle(GetResumenGeneralCajasQuery request, CancellationToken cancellationToken)
        {
            var detalle = (await _repository.ObtenerResumenGeneralCajasAsync(request.AnioLectivo, request.Nivel)).ToList();

            return new ResumenGeneralCajasConsolidadasDto
            {
                TotalIngresosInstitucional = detalle.Sum(x => x.TotalIngresos),
                TotalEgresosInstitucional = detalle.Sum(x => x.TotalEgresos),
                SaldoNetoInstitucional = detalle.Sum(x => x.SaldoNeto),
                TotalAulas = detalle.Count,
                AulasAlDia = detalle.Count(x => x.EstadoFinanciero == "AL_DIA"),
                AulasSinMovimiento = detalle.Count(x => x.EstadoFinanciero == "SIN_MOVIMIENTO"),
                AulasEnAlerta = detalle.Count(x => x.EstadoFinanciero == "ALERTA_ROJO"),
                DetalleAulas = detalle
            };
        }
    }
}
