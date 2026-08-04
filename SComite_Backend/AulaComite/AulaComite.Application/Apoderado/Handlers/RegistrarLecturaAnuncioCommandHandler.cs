using AulaComite.Application.Apoderado.Commands;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Handlers
{
    public class RegistrarLecturaAnuncioCommandHandler : IRequestHandler<RegistrarLecturaAnuncioCommand, bool>
    {
        private readonly IApoderadoRepository _repository;
        private readonly IUserContextService _userContextService;

        public RegistrarLecturaAnuncioCommandHandler(IApoderadoRepository repository, IUserContextService userContextService)
        {
            _repository = repository;
            _userContextService = userContextService;
        }

        public async Task<bool> Handle(RegistrarLecturaAnuncioCommand request, CancellationToken cancellationToken)
        {
            var esApoderadoDelHijo = await ApoderadoAccessValidator.EsEstudianteDelApoderadoAsync(
                _repository, _userContextService, request.EstudianteId, request.AnioLectivo);

            if (!esApoderadoDelHijo)
            {
                return false;
            }

            var usuario = _userContextService.ObtenerUsuarioActual();
            await _repository.RegistrarLecturaAnuncioAsync(request.AnuncioId, request.EstudianteId, usuario);
            return true;
        }
    }
}
