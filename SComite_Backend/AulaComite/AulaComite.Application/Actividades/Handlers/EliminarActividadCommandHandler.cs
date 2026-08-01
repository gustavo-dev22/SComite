using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Actividades.Commands;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Actividades.Handlers
{
    public class EliminarActividadCommandHandler : IRequestHandler<EliminarActividadCommand, bool>
    {
        private readonly IActividadRepository _repository;

        public EliminarActividadCommandHandler(IActividadRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(EliminarActividadCommand request, CancellationToken cancellationToken)
        {
            return await _repository.EliminarAsync(request.Id, request.AulaId);
        }
    }
}
