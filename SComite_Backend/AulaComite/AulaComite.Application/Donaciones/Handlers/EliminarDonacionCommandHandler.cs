using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Donaciones.Commands;
using MediatR;

namespace AulaComite.Application.Donaciones.Handlers
{
    public class EliminarDonacionCommandHandler : IRequestHandler<EliminarDonacionCommand, bool>
    {
        private readonly IDonacionRepository _repository;

        public EliminarDonacionCommandHandler(IDonacionRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(EliminarDonacionCommand request, CancellationToken cancellationToken)
        {
            return await _repository.EliminarAsync(request.Id, request.AulaId);
        }
    }
}
