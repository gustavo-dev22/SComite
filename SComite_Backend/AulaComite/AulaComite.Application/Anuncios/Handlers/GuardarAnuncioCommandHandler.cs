using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Anuncios.Commands;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.Anuncios.Handlers
{
    public class GuardarAnuncioCommandHandler : IRequestHandler<GuardarAnuncioCommand, int>
    {
        private readonly IAnuncioRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GuardarAnuncioCommandHandler(IAnuncioRepository repository, IComiteRepository comiteRepository, IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<int> Handle(GuardarAnuncioCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: el anuncio debe corresponder a un Aula asignada al usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            // Auditoría derivada exclusivamente del token JWT autenticado, nunca del cuerpo JSON.
            string usuarioRegistro = _userContextService.ObtenerUsuarioActual();

            return await _repository.GuardarAsync(
                request.Id, request.AulaId, request.Titulo, request.Contenido,
                request.Categoria, request.EsFijado, request.UrlAdjunto, usuarioRegistro
            );
        }
    }
}
