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
            // 🛡️ T4/IDOR: se verifica PRIMERO la existencia del recurso. Si no existe -> 404 (false).
            var anuncio = await _repository.ObtenerPorIdAsync(request.Id);
            if (anuncio == null)
                return false;

            // 🛡️ Se valida el AulaId REAL del recurso (nunca el AulaId enviado por el cliente),
            // de modo que un usuario sin acceso al Aula del anuncio reciba 403.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, anuncio.AulaId);

            return await _repository.EliminarAsync(request.Id, anuncio.AulaId);
        }
    }
}
