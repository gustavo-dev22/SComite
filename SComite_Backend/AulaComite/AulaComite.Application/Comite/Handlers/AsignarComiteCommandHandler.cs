using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ISasiAuthService _sasiAuthService;

        public AsignarComiteCommandHandler(IComiteRepository repository, IAulaRepository aulaRepository, ILogRepository logRepository, IDbConnectionFactory connectionFactory, IUserContextService userContextService, ISasiAuthService sasiAuthService)
        {
            _repository = repository;
            _aulaRepository = aulaRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
            _userContextService = userContextService;
            _sasiAuthService = sasiAuthService;
        }

        public async Task<int> Handle(AsignarComiteCommand request, CancellationToken cancellationToken)
        {
            // 🛡️ Validar pertenencia: solo se puede asignar integrantes al Aula del usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_repository, _userContextService, request.AulaId);

            // 🛡️ SASI-DOWN/IDOR (crítico): se valida en el SERVIDOR que el UsuarioIdSasi
            // corresponde a un apoderado REAL del catálogo SASI en el momento de la asignación.
            // Si SASI está caído, ObtenerApoderadosAsync lanza SasiNoDisponibleException -> 503
            // y NO se asigna nada (evita asignar con datos desactualizados o forjados).
            // Además se toman los datos (nombre/email) del catálogo REAL de SASI, no del cliente.
            var apoderadosSasi = (await _sasiAuthService.ObtenerApoderadosAsync()).ToList();
            var apoderadoSasi = apoderadosSasi.FirstOrDefault(a =>
                string.Equals(a.UsuarioId, request.UsuarioIdSasi, StringComparison.OrdinalIgnoreCase));

            if (apoderadoSasi == null)
            {
                throw new FluentValidation.ValidationException(
                    "El apoderado seleccionado no está registrado en el servicio SASI. No se puede asignar el cargo.");
            }

            var integrante = new ComiteIntegrante
            {
                AulaId = request.AulaId,
                UsuarioIdSasi = apoderadoSasi.UsuarioId,
                NombreCompleto = apoderadoSasi.NombreCompleto,
                Email = apoderadoSasi.Email,
                Cargo = request.Cargo.ToUpper()
            };

            // 🚀 1. Obtener detalles descriptivos para el mensaje de auditoría
            var aula = await _aulaRepository.ObtenerPorIdAsync(request.AulaId);
            string aulaNombre = aula != null
                ? $"{aula.Nivel} - {aula.Grado}° \"{aula.Seccion}\""
                : $"Aula ID #{request.AulaId}";

            // El nombre/apellido provienen del catálogo REAL de SASI (validado arriba)
            string nombreApoderado = !string.IsNullOrEmpty(apoderadoSasi.NombreCompleto)
                ? apoderadoSasi.NombreCompleto
                : $"Apoderado ({request.UsuarioIdSasi})";

            // 🚀 2. Construir el mensaje legible
            string mensajeLegible = $"Se asignó el cargo de '{request.Cargo.ToUpper()}' al apoderado \"{nombreApoderado}\" para el Aula {aulaNombre}.";

            int id = await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                return await _repository.AsignarIntegranteAsync(integrante, transaction);
            });

            // 🛡️ M13: El log se registra de forma independiente, fuera de la transacción de negocio.
            await _logRepository.RegistrarAsync(
                nivel: "INFO",
                modulo: "COMITE",
                accion: "ASIGNAR_CARGO",
                mensaje: mensajeLegible
            );

            return id;
        }
    }
}
