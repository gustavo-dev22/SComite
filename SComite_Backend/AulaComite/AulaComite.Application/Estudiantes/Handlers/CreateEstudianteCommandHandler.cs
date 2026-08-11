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

        public CreateEstudianteCommandHandler(IEstudianteRepository repository, IAulaRepository aulaRepository, ILogRepository logRepository, IDbConnectionFactory connectionFactory)
        {
            _repository = repository;
            _aulaRepository = aulaRepository;
            _logRepository = logRepository;
            _connectionFactory = connectionFactory;
        }

        public async Task<int> Handle(CreateEstudianteCommand request, CancellationToken cancellationToken)
        {
            RechazarDatosEnmascarados(request.NumeroDocumento, request.TelefonoApoderado);

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
                return await _repository.CrearEstudianteAsync(e, transaction);
            });

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
