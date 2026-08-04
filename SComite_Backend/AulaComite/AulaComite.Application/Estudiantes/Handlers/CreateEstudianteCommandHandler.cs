using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Estudiantes.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Estudiantes.Handlers
{
    public class CreateEstudianteCommandHandler : IRequestHandler<CreateEstudianteCommand, int>
    {
        private readonly IEstudianteRepository _repository;
        private readonly IAulaRepository _aulaRepository;
        private readonly ILogRepository _logRepository;
        private readonly IDbConnectionFactory _connectionFactory;

        public CreateEstudianteCommandHandler(IEstudianteRepository repository, IAulaRepository aulaRepository, ILogRepository logRepository, IDbConnectionFactory connectionFactory)
        {
            _repository = repository;
            _aulaRepository = aulaRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<int> Handle(CreateEstudianteCommand request, CancellationToken cancellationToken)
        {
            var e = new Estudiante
            {
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

            // 🚀 1. Obtener los datos del Aula para construir el display unificado
            var aula = await _aulaRepository.ObtenerPorIdAsync(request.AulaId);
            string aulaDisplay = aula != null
                ? $"{aula.Nivel} - {aula.Grado}° \"{aula.Seccion}\""
                : $"Aula ID #{request.AulaId}";

            // 🚀 2. Armar el mensaje legible con el Apoderado y el Aula
            string mensajeLegible = $"Se registró al estudiante {request.ApellidoPaterno} {request.ApellidoMaterno}, {request.Nombres} ({request.TipoDocumento}: {request.NumeroDocumento}) con apoderado \"{request.NombreApoderado}\" en el Aula {aulaDisplay}.";

            int id = await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                int estudianteId = await _repository.CrearEstudianteAsync(e, transaction);

                await _logRepository.RegistrarAsync(
                    nivel: "INFO",
                    modulo: "ESTUDIANTES",
                    accion: "CREAR_ESTUDIANTE",
                    mensaje: mensajeLegible,
                    transaction: transaction
                );

                return estudianteId;
            });

            return id;
        }
    }
}
