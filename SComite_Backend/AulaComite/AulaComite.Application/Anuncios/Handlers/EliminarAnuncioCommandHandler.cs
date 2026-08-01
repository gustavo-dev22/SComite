using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Anuncios.Commands;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Anuncios.Handlers
{
    public class EliminarAnuncioCommandHandler : IRequestHandler<EliminarAnuncioCommand, bool>
    {
        private readonly IAnuncioRepository _repository;

        public EliminarAnuncioCommandHandler(IAnuncioRepository repository) => _repository = repository;

        public async Task<bool> Handle(EliminarAnuncioCommand request, CancellationToken cancellationToken)
        {
            return await _repository.EliminarAsync(request.Id, request.AulaId);
        }
    }
}
