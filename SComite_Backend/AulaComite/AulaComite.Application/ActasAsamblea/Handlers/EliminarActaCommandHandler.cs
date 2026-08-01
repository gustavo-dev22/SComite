using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.ActasAsamblea.Commands;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Handlers
{
    public class EliminarActaCommandHandler : IRequestHandler<EliminarActaCommand, bool>
    {
        private readonly IActaAsambleaRepository _repository;

        public EliminarActaCommandHandler(IActaAsambleaRepository repository) => _repository = repository;

        public async Task<bool> Handle(EliminarActaCommand request, CancellationToken cancellationToken)
        {
            return await _repository.EliminarAsync(request.Id, request.AulaId);
        }
    }
}
