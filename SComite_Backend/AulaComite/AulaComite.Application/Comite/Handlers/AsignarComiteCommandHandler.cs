using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Comite.Commands;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Comite.Handlers
{
    public class AsignarComiteCommandHandler : IRequestHandler<AsignarComiteCommand, int>
    {
        private readonly IComiteRepository _repository;
        private readonly IAulaRepository _aulaRepository;
        private readonly ILogRepository _logRepository;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUserContextService _userContextService;

        public AsignarComiteCommandHandler(IComiteRepository repository, IAulaRepository aulaRepository, ILogRepository logRepository, IDbConnectionFactory connectionFactory, IUserContextService userContextService)
        {
            _repository = repository;
            _aulaRepository = aulaRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
            _userContextService = userContextService;
        }

        public async Task<int> Handle(AsignarComiteCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: solo se puede asignar integrantes al Aula del usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_repository, _userContextService, request.AulaId);

            var integrante = new ComiteIntegrante
            {
                AulaId = request.AulaId,
                UsuarioIdSasi = request.UsuarioIdSasi,
                NombreCompleto = request.NombreCompleto,
                Email = request.Email,
                Cargo = request.Cargo.ToUpper()
            };

            // 🚀 1. Obtener detalles descriptivos para el mensaje de auditoría
            var aula = await _aulaRepository.ObtenerPorIdAsync(request.AulaId);
            string aulaNombre = aula != null
                ? $"{aula.Nivel} - {aula.Grado}° \"{aula.Seccion}\""
                : $"Aula ID #{request.AulaId}";

            // Si el request ya trae el NombreCompletoApoderado desde el Frontend:
            string nombreApoderado = !string.IsNullOrEmpty(request.NombreCompleto)
                ? request.NombreCompleto
                : $"Apoderado ({request.UsuarioIdSasi})";

            // 🚀 2. Construir el mensaje legible
            string mensajeLegible = $"Se asignó el cargo de '{request.Cargo.ToUpper()}' al apoderado \"{nombreApoderado}\" para el Aula {aulaNombre}.";

            int id = await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                int integranteId = await _repository.AsignarIntegranteAsync(integrante, transaction);

                await _logRepository.RegistrarAsync(
                    nivel: "INFO",
                    modulo: "COMITE",
                    accion: "ASIGNAR_CARGO",
                    mensaje: mensajeLegible,
                    transaction: transaction
                );

                return integranteId;
            });

            return id;
        }
    }
}
