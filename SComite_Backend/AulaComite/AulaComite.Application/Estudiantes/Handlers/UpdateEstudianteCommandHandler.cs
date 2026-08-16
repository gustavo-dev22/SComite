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

        public UpdateEstudianteCommandHandler(IEstudianteRepository repository, IUserContextService userContextService)
        {
            _repository = repository;
            _userContextService = userContextService;
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
                request.UsuarioIdApoderadoSasi,
                request.NombreApoderado,
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
