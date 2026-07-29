using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Comite.Commands;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Comite.Handlers
{
    public class DeleteComiteCommandHandler : IRequestHandler<DeleteComiteCommand, bool>
    {
        private readonly IComiteRepository _repository;

        public DeleteComiteCommandHandler(IComiteRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteComiteCommand request, CancellationToken cancellationToken)
        {
            return await _repository.EliminarIntegranteAsync(request.Id);
        }
    }
}
