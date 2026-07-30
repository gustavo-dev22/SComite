using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Cuotas.Queries;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class GetDetalleCobroEstudiantesQueryHandler : IRequestHandler<GetDetalleCobroEstudiantesQuery, IEnumerable<CuotaEstudianteCobro>>
    {
        private readonly ICuotaRepository _repository;

        public GetDetalleCobroEstudiantesQueryHandler(ICuotaRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CuotaEstudianteCobro>> Handle(GetDetalleCobroEstudiantesQuery request, CancellationToken cancellationToken)
        {
            return await _repository.ObtenerDetalleCobroEstudiantesAsync(request.CuotaId);
        }
    }
}
