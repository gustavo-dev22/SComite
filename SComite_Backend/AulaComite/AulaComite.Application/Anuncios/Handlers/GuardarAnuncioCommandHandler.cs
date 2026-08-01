using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Anuncios.Commands;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Anuncios.Handlers
{
    public class GuardarAnuncioCommandHandler : IRequestHandler<GuardarAnuncioCommand, int>
    {
        private readonly IAnuncioRepository _repository;

        public GuardarAnuncioCommandHandler(IAnuncioRepository repository) => _repository = repository;

        public async Task<int> Handle(GuardarAnuncioCommand request, CancellationToken cancellationToken)
        {
            return await _repository.GuardarAsync(
                request.Id, request.AulaId, request.Titulo, request.Contenido,
                request.Categoria, request.EsFijado, request.UrlAdjunto, request.UsuarioRegistro
            );
        }
    }
}
