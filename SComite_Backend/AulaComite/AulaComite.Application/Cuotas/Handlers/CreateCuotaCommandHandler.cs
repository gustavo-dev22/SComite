using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Cuotas.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class CreateCuotaCommandHandler : IRequestHandler<CreateCuotaCommand, int>
    {
        private readonly ICuotaRepository _cuotaRepository;
        private readonly IAulaRepository _aulaRepository;
        private readonly ILogRepository _logRepository;
        private readonly IDbConnectionFactory _connectionFactory;

        public CreateCuotaCommandHandler(
            ICuotaRepository cuotaRepository,
            IAulaRepository aulaRepository,
            ILogRepository logRepository,
            IDbConnectionFactory connectionFactory)
        {
            _cuotaRepository = cuotaRepository;
            _aulaRepository = aulaRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<int> Handle(CreateCuotaCommand request, CancellationToken cancellationToken)
        {
            var cuota = new Cuota
            {
                AulaId = request.AulaId,
                Concepto = request.Concepto,
                MontoIndividual = request.MontoIndividual,
                FechaVencimiento = request.FechaVencimiento,
                Observacion = request.Observacion,
                ActividadId = request.ActividadId
            };

            // Obtener datos del aula para el Log legible
            var aula = await _aulaRepository.ObtenerPorIdAsync(request.AulaId);
            string aulaDisplay = aula != null ? $"{aula.Nivel} - {aula.Grado}° \"{aula.Seccion}\"" : $"Aula #{request.AulaId}";

            int id = await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                int cuotaId = await _cuotaRepository.CrearCuotaMasivaAsync(cuota, transaction);

                await _logRepository.RegistrarAsync(
                    nivel: "INFO",
                    modulo: "TESORERIA",
                    accion: "CREAR_CUOTA",
                    mensaje: $"Se aperturó la cuota '{request.Concepto.ToUpper()}' por S/. {request.MontoIndividual:F2} para el Aula {aulaDisplay} (Vence: {request.FechaVencimiento:dd/MM/yyyy}).",
                    transaction: transaction
                );

                return cuotaId;
            });

            return id;
        }
    }
}
