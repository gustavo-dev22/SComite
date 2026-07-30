using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Gastos.Commands;
using MediatR;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Gastos.Handlers
{
    public class CreateGastoCommandHandler : IRequestHandler<CreateGastoCommand, int>
    {
        private readonly IGastoRepository _gastoRepository;
        private readonly IAulaRepository _aulaRepository;
        private readonly IUserContextService _userContextService;
        private readonly ILogRepository _logRepository;

        public CreateGastoCommandHandler(
            IGastoRepository gastoRepository,
            IAulaRepository aulaRepository,
            IUserContextService userContextService,
            ILogRepository logRepository)
        {
            _gastoRepository = gastoRepository;
            _aulaRepository = aulaRepository;
            _userContextService = userContextService;
            _logRepository = logRepository;
        }

        public async Task<int> Handle(CreateGastoCommand request, CancellationToken cancellationToken)
        {
            string usuario = _userContextService.ObtenerUsuarioActual();

            var gasto = new GastoComite
            {
                AulaId = request.AulaId,
                Concepto = request.Concepto,
                Categoria = request.Categoria,
                Monto = request.Monto,
                FechaGasto = request.FechaGasto,
                TipoComprobante = request.TipoComprobante,
                NumeroComprobante = request.NumeroComprobante,
                Proveedor = request.Proveedor,
                Observacion = request.Observacion,
                UsuarioRegistro = usuario
            };

            int id = await _gastoRepository.RegistrarAsync(gasto);

            var aula = await _aulaRepository.ObtenerPorIdAsync(request.AulaId);
            string aulaDisplay = aula != null ? $"{aula.Nivel} - {aula.Grado}° \"{aula.Seccion}\"" : $"Aula #{request.AulaId}";

            await _logRepository.RegistrarAsync(
                nivel: "INFO",
                modulo: "TESORERIA",
                accion: "REGISTRAR_GASTO",
                mensaje: $"Se registró el egreso '{request.Concepto.ToUpper()}' por S/. {request.Monto:F2} ({request.Categoria}) en el Aula {aulaDisplay}."
            );

            return id;
        }
    }
}
