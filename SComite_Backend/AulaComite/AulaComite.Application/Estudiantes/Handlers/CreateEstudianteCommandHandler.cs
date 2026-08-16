using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Estudiantes.Commands;
using AulaComite.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace AulaComite.Application.Estudiantes.Handlers
{
    public class CreateEstudianteCommandHandler : IRequestHandler<CreateEstudianteCommand, int>
    {
        private readonly IEstudianteRepository _repository;
        private readonly IAulaRepository _aulaRepository;
        private readonly ILogRepository _logRepository;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ISasiAuthService _sasiAuthService;

        public CreateEstudianteCommandHandler(IEstudianteRepository repository, IAulaRepository aulaRepository, ILogRepository logRepository, IDbConnectionFactory connectionFactory, ISasiAuthService sasiAuthService)
        {
            _repository = repository;
            _aulaRepository = aulaRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
            _sasiAuthService = sasiAuthService;
        }

        public async Task<int> Handle(CreateEstudianteCommand request, CancellationToken cancellationToken)
        {
            RechazarDatosEnmascarados(request.NumeroDocumento, request.TelefonoApoderado);

            // 🛡️ SASI-DOWN/IDOR (crítico): si se envía un UsuarioIdApoderadoSasi, se valida en el
            // SERVIDOR que corresponde a un apoderado REAL del catálogo SASI en el momento del
            // registro. Si SASI está caído, ObtenerApoderadosAsync lanza SasiNoDisponibleException
            // -> 503 y NO se persiste (evita vincular con datos desactualizados o forjados).
            // El apoderado es OPCIONAL: si no se envía, se permite guardar sin vínculo.
            string? usuarioIdApoderado = request.UsuarioIdApoderadoSasi;
            string? nombreApoderado = request.NombreApoderado;

            if (!string.IsNullOrWhiteSpace(usuarioIdApoderado))
            {
                var apoderadosSasi = (await _sasiAuthService.ObtenerApoderadosAsync()).ToList();
                var apoderadoSasi = apoderadosSasi.FirstOrDefault(a =>
                    string.Equals(a.UsuarioId, usuarioIdApoderado, StringComparison.OrdinalIgnoreCase));

                if (apoderadoSasi == null)
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure(nameof(CreateEstudianteCommand.UsuarioIdApoderadoSasi),
                            "El apoderado seleccionado no está registrado en el servicio SASI. Verifique el vínculo o guarde sin apoderado.")
                    });
                }

                // Se toman los datos del catálogo REAL de SASI, no del cliente.
                usuarioIdApoderado = apoderadoSasi.UsuarioId;
                nombreApoderado = apoderadoSasi.NombreCompleto;
            }

            var e = new Estudiante
            {
                AulaId = request.AulaId,
                TipoDocumento = request.TipoDocumento,
                NumeroDocumento = request.NumeroDocumento,
                Nombres = request.Nombres,
                ApellidoPaterno = request.ApellidoPaterno,
                ApellidoMaterno = request.ApellidoMaterno,
                UsuarioIdApoderadoSasi = usuarioIdApoderado,
                NombreApoderado = nombreApoderado,
                TelefonoApoderado = request.TelefonoApoderado
            };

            // 🚀 1. Obtener los datos del Aula para construir el display unificado
            var aula = await _aulaRepository.ObtenerPorIdAsync(request.AulaId);
            string aulaDisplay = aula != null
                ? $"{aula.Nivel} - {aula.Grado}° \"{aula.Seccion}\""
                : $"Aula ID #{request.AulaId}";

            // 🚀 2. Armar el mensaje legible con el Apoderado y el Aula.
            // 🛡️ M7/PII: el documento se registra ENMASCARADO (nunca completo) en el log.
            string mensajeLegible = $"Se registró al estudiante {request.ApellidoPaterno} {request.ApellidoMaterno}, {request.Nombres} ({request.TipoDocumento}: {PiiMasker.EnmascararDocumento(request.NumeroDocumento)}) con apoderado \"{nombreApoderado}\" en el Aula {aulaDisplay}.";

            int id = await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                return await _repository.CrearEstudianteAsync(e, transaction);
            });

            // Identificador del registro para trazabilidad sin exponer el documento completo.
            mensajeLegible += $" | EstudianteId: {id}";

            // 🛡️ M13: El log se registra de forma independiente, fuera de la transacción de negocio.
            await _logRepository.RegistrarAsync(
                nivel: "INFO",
                modulo: "ESTUDIANTES",
                accion: "CREAR_ESTUDIANTE",
                mensaje: mensajeLegible
            );

            return id;
        }

        /// <summary>
        /// 🛡️ M7: Si el cliente envió datos previamente enmascarados (ej. DNI "12****45",
        /// teléfono "987****21") el registro se rechaza con 400, evitando persistir el
        /// formato enmascarado como valor real.
        /// </summary>
        private static void RechazarDatosEnmascarados(params string?[] valores)
        {
            var camposEnmascarados = new List<string>();

            if (PiiMasker.EsDatoEnmascarado(valores[0])) camposEnmascarados.Add("NumeroDocumento");
            if (PiiMasker.EsDatoEnmascarado(valores[1])) camposEnmascarados.Add("TelefonoApoderado");

            if (camposEnmascarados.Count > 0)
            {
                throw new ValidationException(camposEnmascarados.Select(campo =>
                    new ValidationFailure(campo, "Los datos enviados contienen formato enmascarado inválido.")));
            }
        }
    }
}
