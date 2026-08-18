using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Estudiantes.Commands;
using AulaComite.Application.Estudiantes.Dtos;
using MediatR;

namespace AulaComite.Application.Estudiantes.Handlers
{
    public class MigrarEstudiantesCommandHandler : IRequestHandler<MigrarEstudiantesCommand, ResultadoMigracionDto>
    {
        private readonly IEstudianteRepository _estudianteRepository;

        public MigrarEstudiantesCommandHandler(IEstudianteRepository estudianteRepository)
        {
            _estudianteRepository = estudianteRepository;
        }

        public async Task<ResultadoMigracionDto> Handle(MigrarEstudiantesCommand request, CancellationToken cancellationToken)
        {
            return await _estudianteRepository.MigrarEstudiantesAsync(request.AulaDestinoId, request.EstudianteIds);
        }
    }
}
