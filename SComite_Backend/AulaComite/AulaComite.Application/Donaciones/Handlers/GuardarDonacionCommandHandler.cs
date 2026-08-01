using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Donaciones.Commands;
using MediatR;

namespace AulaComite.Application.Donaciones.Handlers
{
    public class GuardarDonacionCommandHandler : IRequestHandler<GuardarDonacionCommand, int>
    {
        private readonly IDonacionRepository _repository;

        public GuardarDonacionCommandHandler(IDonacionRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> Handle(GuardarDonacionCommand request, CancellationToken cancellationToken)
        {
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
