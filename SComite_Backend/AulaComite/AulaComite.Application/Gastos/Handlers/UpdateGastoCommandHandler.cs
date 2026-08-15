using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using AulaComite.Application.Gastos.Commands;
using AulaComite.Domain.Entities;
using MediatR;

namespace AulaComite.Application.Gastos.Handlers
{
    public class UpdateGastoCommandHandler : IRequestHandler<UpdateGastoCommand, bool>
    {
        private readonly IGastoRepository _gastoRepository;
        private readonly IComiteRepository _comiteRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IUserContextService _currentUserService;

        public UpdateGastoCommandHandler(IGastoRepository gastoRepository, IComiteRepository comiteRepository, IFileStorageService fileStorageService, IUserContextService currentUserService)
        {
            _gastoRepository = gastoRepository;
            _comiteRepository = comiteRepository;
            _fileStorageService = fileStorageService;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(UpdateGastoCommand request, CancellationToken cancellationToken)
        {
            var usuarioActual = _currentUserService.ObtenerUsuarioActual() ?? "SISTEMA";

            // 1. Obtener la información actual del gasto en BD
            var gastoExistente = await _gastoRepository.ObtenerPorIdAsync(request.Id);
            if (gastoExistente == null) return false;

            // 🛡️ Validar pertenencia: el gasto debe pertenecer a un Aula asignada al usuario.
            await AulaAccessValidator.ValidarAccesoAulaAsync(_comiteRepository, _currentUserService, gastoExistente.AulaId);

            // 🛡️ Integridad financiera: el AulaId es INMUTABLE. Se prohíbe trasladar un gasto
            // a otra aula (evita descuadres de caja entre aulas).
            if (request.AulaId != gastoExistente.AulaId)
            {
                throw new FluentValidation.ValidationException(
                    "No se permite trasladar un gasto a otra aula. El AulaId no puede modificarse.");
            }

            // 2. Si se adjuntó un nuevo comprobante distinto al previo, eliminar el archivo antiguo
            if (!string.IsNullOrEmpty(gastoExistente.UrlComprobante) &&
                !string.IsNullOrEmpty(request.UrlComprobante) &&
                gastoExistente.UrlComprobante != request.UrlComprobante)
            {
                _fileStorageService.EliminarComprobante(gastoExistente.UrlComprobante);
            }

            var gasto = new GastoComite
            {
                Id = request.Id,
                AulaId = gastoExistente.AulaId,
                Concepto = request.Concepto,
                Categoria = request.Categoria,
                Monto = request.Monto,
                FechaGasto = request.FechaGasto,
                TipoComprobante = request.TipoComprobante,
                NumeroComprobante = request.NumeroComprobante,
                Proveedor = request.Proveedor,
                Observacion = request.Observacion,
                UrlComprobante = request.UrlComprobante,
                UsuarioRegistro = usuarioActual
            };

            return await _gastoRepository.ActualizarAsync(gasto);
        }
    }
}
