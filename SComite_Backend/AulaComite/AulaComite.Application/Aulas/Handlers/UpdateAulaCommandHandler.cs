using AulaComite.Application.Aulas.Commands;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Aulas.Handlers
{
    public class UpdateAulaCommandHandler : IRequestHandler<UpdateAulaCommand, bool>
    {
        private readonly IAulaRepository _aulaRepository;

        public UpdateAulaCommandHandler(IAulaRepository aulaRepository)
        {
            _aulaRepository = aulaRepository;
        }

        public async Task<bool> Handle(UpdateAulaCommand request, CancellationToken cancellationToken)
        {
            var aula = new Domain.Entities.Aula
            {
                Id = request.Id,
                PeriodoId = request.PeriodoId,
                Nivel = request.Nivel.ToUpper(),
                Grado = request.Grado.ToUpper(),
                Seccion = request.Seccion.ToUpper()
            };

            return await _aulaRepository.ActualizarAulaAsync(aula);
        }
    }
}
