using AulaComite.Application.Aulas.Commands;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Aulas.Handlers
{
    public class DeleteAulaCommandHandler : IRequestHandler<DeleteAulaCommand, bool>
    {
        private readonly IAulaRepository _aulaRepository;

        public DeleteAulaCommandHandler(IAulaRepository aulaRepository)
        {
            _aulaRepository = aulaRepository;
        }

        public async Task<bool> Handle(DeleteAulaCommand request, CancellationToken cancellationToken)
        {
            return await _aulaRepository.EliminarAulaLogicoAsync(request.Id);
        }
    }
}
