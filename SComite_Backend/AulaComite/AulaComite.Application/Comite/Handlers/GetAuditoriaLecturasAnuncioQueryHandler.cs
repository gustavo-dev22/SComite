using AulaComite.Application.Comite.Dtos;
using AulaComite.Application.Comite.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Comite.Handlers
{
    public class GetAuditoriaLecturasAnuncioQueryHandler : IRequestHandler<GetAuditoriaLecturasAnuncioQuery, ResumenAuditoriaAnuncioDto>
    {
        private readonly IAnuncioRepository _repository;

        public GetAuditoriaLecturasAnuncioQueryHandler(IAnuncioRepository repository) => _repository = repository;

        public async Task<ResumenAuditoriaAnuncioDto> Handle(GetAuditoriaLecturasAnuncioQuery request, CancellationToken cancellationToken)
        {
            var lista = (await _repository.ObtenerAuditoriaLecturasAsync(request.AnuncioId)).ToList();

            return new ResumenAuditoriaAnuncioDto
            {
                AnuncioId = request.AnuncioId,
                TotalEstudiantesAula = lista.Count,
                TotalLeidos = lista.Count(x => x.Leido),
                TotalPendientes = lista.Count(x => !x.Leido),
                Lecturas = lista
            };
        }
    }
}
