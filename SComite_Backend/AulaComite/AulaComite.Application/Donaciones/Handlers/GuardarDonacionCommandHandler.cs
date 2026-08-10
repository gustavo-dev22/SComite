using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Donaciones.Commands;
using MediatR;

namespace AulaComite.Application.Donaciones.Handlers
{
    public class GuardarDonacionCommandHandler : IRequestHandler<GuardarDonacionCommand, int>
    {
        private readonly IDonacionRepository _repository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IUserContextService _userContextService;

        public GuardarDonacionCommandHandler(IDonacionRepository repository, IComiteRepository comiteRepository, IUserContextService userContextService)
        {
            _repository = repository;
            _comiteRepository = comiteRepository;
            _userContextService = userContextService;
        }

        public async Task<int> Handle(GuardarDonacionCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: la donación debe corresponder a un Aula asignada al usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

            return await _repository.GuardarAsync(
                request.Id,
                request.AulaId,
                request.Donante,
                request.Monto,
                request.FechaDonacion,
                request.Concepto,
                request.Observacion
            );
        }
    }
}
