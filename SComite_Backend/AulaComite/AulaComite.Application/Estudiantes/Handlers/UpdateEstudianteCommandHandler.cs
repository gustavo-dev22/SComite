using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Estudiantes.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Estudiantes.Handlers
{
    public class UpdateEstudianteCommandHandler : IRequestHandler<UpdateEstudianteCommand, bool>
    {
        private readonly IEstudianteRepository _repository;

        public UpdateEstudianteCommandHandler(IEstudianteRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(UpdateEstudianteCommand request, CancellationToken cancellationToken)
        {
            var e = new Estudiante
            {
                Id = request.Id,
                AulaId = request.AulaId,
                TipoDocumento = request.TipoDocumento,
                NumeroDocumento = request.NumeroDocumento,
                Nombres = request.Nombres,
                ApellidoPaterno = request.ApellidoPaterno,
                ApellidoMaterno = request.ApellidoMaterno,
                UsuarioIdApoderadoSasi = request.UsuarioIdApoderadoSasi,
                NombreApoderado = request.NombreApoderado,
                TelefonoApoderado = request.TelefonoApoderado
            };

            return await _repository.ActualizarEstudianteAsync(e);
        }
    }
}
