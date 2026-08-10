using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Cuotas.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Cuotas.Handlers
{
    public class CreateCuotaCommandHandler : IRequestHandler<CreateCuotaCommand, int>
    {
        private readonly ICuotaRepository _cuotaRepository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IAulaRepository _aulaRepository;
        private readonly ILogRepository _logRepository;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUserContextService _userContextService;

        public CreateCuotaCommandHandler(
            ICuotaRepository cuotaRepository,
            IComiteRepository comiteRepository,
            IAulaRepository aulaRepository,
            ILogRepository logRepository,
            IDbConnectionFactory connectionFactory,
            IUserContextService userContextService)
        {
            _cuotaRepository = cuotaRepository;
            _comiteRepository = comiteRepository;
            _aulaRepository = aulaRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
            _userContextService = userContextService;
        }

        public async Task<int> Handle(CreateCuotaCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: la cuota debe crearse en un Aula asignada al usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _userContextService, request.AulaId);

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
