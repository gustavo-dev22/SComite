using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.ActasAsamblea.Commands;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.ActasAsamblea.Handlers
{
    public class GuardarActaCommandHandler : IRequestHandler<GuardarActaCommand, int>
    {
        private readonly IActaAsambleaRepository _repository;

        public GuardarActaCommandHandler(IActaAsambleaRepository repository) => _repository = repository;

        public async Task<int> Handle(GuardarActaCommand request, CancellationToken cancellationToken)
        {
            return await _repository.GuardarAsync(
                request.Id, request.AulaId, request.NumeroActa, request.Titulo,
                request.FechaReunion, request.AgendaAcuerdos, request.EstadoActa,
                request.UrlDocumentoPdf, request.UsuarioRegistro
            );
        }
    }
}
