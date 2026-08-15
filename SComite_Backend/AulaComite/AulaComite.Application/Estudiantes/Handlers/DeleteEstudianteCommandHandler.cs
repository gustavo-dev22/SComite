using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Estudiantes.Commands;
using MediatR;

namespace AulaComite.Application.Estudiantes.Handlers
{
    public class DeleteEstudianteCommandHandler : IRequestHandler<DeleteEstudianteCommand, bool>
    {
        private readonly IEstudianteRepository _repository;
        private readonly ILogRepository _logRepository;
        private readonly IDbConnectionFactory _connectionFactory;

        public DeleteEstudianteCommandHandler(IEstudianteRepository repository, ILogRepository logRepository, IDbConnectionFactory connectionFactory)
        {
            _repository = repository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> Handle(DeleteEstudianteCommand request, CancellationToken cancellationToken)
        {
            var estudiante = await _repository.ObtenerPorIdAsync(request.Id);

            bool resultado = await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                return await _repository.EliminarEstudianteLogicoAsync(request.Id, transaction);
            });

            // 🛡️ M13: El log se registra de forma independiente, fuera de la transacción de negocio.
            if (resultado)
            {
                string nombreEstudiante = estudiante != null && !string.IsNullOrWhiteSpace(estudiante.NombreCompleto)
                    ? estudiante.NombreCompleto
                    : $"Estudiante #{request.Id}";

                await _logRepository.RegistrarAsync(
                    nivel: "WARNING",
                    modulo: "ESTUDIANTES",
                    accion: "DESACTIVAR_ESTUDIANTE",
                    mensaje: $"El estudiante {nombreEstudiante} fue cambiado a estado Inactivo/Retirado."
                );
            }

            return resultado;
        }
    }
}
