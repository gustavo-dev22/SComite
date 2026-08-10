using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.ActasAsamblea.Commands;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Handlers
{
    public class GuardarActaCommandHandler : IRequestHandler<GuardarActaCommand, int>
    {
        private readonly IActaAsambleaRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GuardarActaCommandHandler(IActaAsambleaRepository repository, IComiteRepository comiteRepository, IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<int> Handle(GuardarActaCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: el acta debe corresponder a un Aula asignada al usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            // Auditoría derivada exclusivamente del token JWT autenticado, nunca del cuerpo JSON.
            string usuarioRegistro = _userContextService.ObtenerUsuarioActual();

            return await _repository.GuardarAsync(
                request.Id, request.AulaId, request.NumeroActa, request.Titulo,
                request.FechaReunion, request.AgendaAcuerdos, request.EstadoActa,
                request.UrlDocumentoPdf, usuarioRegistro
            );
        }
    }
}
