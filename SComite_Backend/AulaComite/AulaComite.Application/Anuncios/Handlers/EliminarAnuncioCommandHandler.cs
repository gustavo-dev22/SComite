using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Anuncios.Commands;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Anuncios.Handlers
{
    public class EliminarAnuncioCommandHandler : IRequestHandler<EliminarAnuncioCommand, bool>
    {
        private readonly IAnuncioRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public EliminarAnuncioCommandHandler(IAnuncioRepository repository, IComiteRepository comiteRepository, IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(EliminarAnuncioCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: el anuncio debe pertenecer a un Aula asignada al usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            return await _repository.EliminarAsync(request.Id, request.AulaId);
        }
    }
}
