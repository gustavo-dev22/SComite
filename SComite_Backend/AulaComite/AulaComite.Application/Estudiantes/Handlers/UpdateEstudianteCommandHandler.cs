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
    public class UpdateEstudianteCommandHandler : IRequestHandler<UpdateEstudianteCommand, bool>
    {
        private readonly IEstudianteRepository _repository;
        private readonly IUserContextService _userContextService;
        private readonly ISasiAuthService _sasiAuthService;

        public UpdateEstudianteCommandHandler(IEstudianteRepository repository, IUserContextService userContextService, ISasiAuthService sasiAuthService)
        {
            _repository = repository;
            _userContextService = userContextService;
            _sasiAuthService = sasiAuthService;
        }

        public async Task<bool> Handle(UpdateEstudianteCommand request, CancellationToken cancellationToken)
        {
            RechazarDatosEnmascarados(request.NumeroDocumento, request.TelefonoApoderado);

            // 🛡️ T3.4: Protección contra Mass Assignment. Un usuario que no sea
            // Administrador Global NO puede trasladar a un estudiante a otra aula
            // cambiando el AulaId en el body; solo puede editar sus datos dentro
            // del aula actual.
            var existente = await _repository.ObtenerPorIdAsync(request.Id);
            if (existente == null)
                return false;

            if (existente.AulaId != request.AulaId && !_userContextService.EsAdministradorGlobal())
            {
                throw new UnauthorizedAccessException(
                    "No tiene permisos para trasladar al estudiante a otra aula. El AulaId no puede modificarse.");
            }

            // 🛡️ SASI-DOWN/IDOR (crítico): si se envía un UsuarioIdApoderadoSasi, se valida en el
            // SERVIDOR que corresponde a un apoderado REAL del catálogo SASI en el momento de la
            // actualización. Si SASI está caído, ObtenerApoderadosAsync lanza SasiNoDisponibleException
            // -> 503 y NO se persiste. El apoderado es OPCIONAL: si no se envía, se conserva sin vínculo.
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
                        new ValidationFailure(nameof(UpdateEstudianteCommand.UsuarioIdApoderadoSasi),
                            "El apoderado seleccionado no está registrado en el servicio SASI. Verifique el vínculo o guarde sin apoderado.")
                    });
                }

                // Se toman los datos del catálogo REAL de SASI, no del cliente.
                usuarioIdApoderado = apoderadoSasi.UsuarioId;
                nombreApoderado = apoderadoSasi.NombreCompleto;
            }

            var e = new Estudiante
            {
                Id = request.Id
            };

            e.ActualizarDatos(
                request.AulaId,
                request.TipoDocumento,
                request.NumeroDocumento,
                request.Nombres,
                request.ApellidoPaterno,
                request.ApellidoMaterno,
                usuarioIdApoderado,
                nombreApoderado,
                request.TelefonoApoderado);

            return await _repository.ActualizarEstudianteAsync(e);
        }

        /// <summary>
        /// 🛡️ M7: Si el cliente devolvió datos previamente enmascarados (ej. DNI "12****45",
        /// teléfono "987****21") la actualización se rechaza con 400, evitando persistir el
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
