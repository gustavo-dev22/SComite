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
            bool resultado = await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                bool eliminado = await _repository.EliminarEstudianteLogicoAsync(request.Id, transaction);

                if (eliminado)
                {
                    await _logRepository.RegistrarAsync(
                        nivel: "WARNING",
                        modulo: "ESTUDIANTES",
                        accion: "DESACTIVAR_ESTUDIANTE",
                        mensaje: $"El estudiante con ID #{request.Id} fue cambiado a estado Inactivo/Retirado.",
                        transaction: transaction
                    );
                }

                return eliminado;
            });

            return resultado;
        }
    }
}
