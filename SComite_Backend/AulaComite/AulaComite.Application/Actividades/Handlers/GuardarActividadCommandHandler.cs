using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Actividades.Commands;
using AulaComite.Application.Common.Interfaces;
using MediatR;

namespace AulaComite.Application.Actividades.Handlers
{
    public class GuardarActividadCommandHandler : IRequestHandler<GuardarActividadCommand, int>
    {
        private readonly IActividadRepository _repository;

        public GuardarActividadCommandHandler(IActividadRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> Handle(GuardarActividadCommand request, CancellationToken cancellationToken)
        {
            return await _repository.GuardarAsync(
                request.Id,
                request.AulaId,
                request.NombreActividad,
                request.Descripcion,
                request.FechaProgramada,
                request.MontoPresupuestado,
                request.CuotaSugeridaPorAlumno,
                request.Estado
            );
        }
    }
}
