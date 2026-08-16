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
            // 🛡️ T4/IDOR: en modo edición (Id > 0) se verifica PRIMERO la existencia y se valida
            // el AulaId REAL del recurso (nunca el AulaId del cliente) -> 404 si no existe, 403 si es ajeno.
            int aulaDestino = request.AulaId;

            if (request.Id > 0)
            {
                var existente = await _repository.ObtenerPorIdAsync(request.Id);
                if (existente == null)
                    throw new KeyNotFoundException("No se encontró el acta a editar.");

                await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, existente.AulaId);
                aulaDestino = existente.AulaId;
            }
            else
            {
                // Creación: el Aula destino debe estar asignada al usuario.
                await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);
            }

            // Auditoría derivada exclusivamente del token JWT autenticado, nunca del cuerpo JSON.
            string usuarioRegistro = _userContextService.ObtenerUsuarioActual();

            // 🛡️ M3: sanitizar Titulo/AgendaAcuerdos como texto plano para prevenir XSS almacenado.
            var tituloSanitizado = XssSanitizer.SanitizarTextoPlano(request.Titulo);
            var agendaSanitizada = XssSanitizer.SanitizarTextoPlano(request.AgendaAcuerdos);

            return await _repository.GuardarAsync(
                request.Id, aulaDestino, request.NumeroActa, tituloSanitizado,
                request.FechaReunion, agendaSanitizada, request.EstadoActa,
                request.UrlDocumentoPdf, usuarioRegistro
            );
        }
    }
}
