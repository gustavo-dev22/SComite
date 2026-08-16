using System;
using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Cuotas.Commands;
using AulaComite.Application.Cuotas.Queries;
using Microsoft.AspNetCore.Authorization;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ManejoFinanciero")]
    public class CuotasController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CuotasController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("aula/{aulaId:int}")]
        public async Task<IActionResult> GetPorAula(int aulaId)
        {
            var result = await _mediator.Send(new GetCuotasPorAulaQuery(aulaId));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CreateCuotaCommand command)
        {
            int id = await _mediator.Send(command);
            return Created($"/api/Cuotas/{id}", new { id, mensaje = "Cuota aperturada y asignada masivamente con éxito." });
        }

        [HttpPost("programacion-mensual")]
        public async Task<IActionResult> ProgramarMensual([FromBody] GenerarCuotasMensualesCommand command)
        {
            bool exito = await _mediator.Send(command);
            return Ok(new { exito, mensaje = "Programación de cuotas mensuales de caja chica generada con éxito para todo el año lectivo." });
        }

        [HttpGet("{cuotaId:int}/cobros")]
        public async Task<IActionResult> GetCobrosPorCuota(int cuotaId)
        {
            var result = await _mediator.Send(new GetDetalleCobroEstudiantesQuery(cuotaId));
            return Ok(result);
        }

        [HttpPost("registrar-pago-manual")]
        public async Task<IActionResult> RegistrarPagoManual([FromBody] RegistrarPagoManualCommand command)
        {
            bool exito = await _mediator.Send(command);
            return Ok(new { exito, mensaje = "Pago registrado correctamente." });
        }

        [HttpPost("anular-pago")]
        public async Task<IActionResult> AnularPago([FromBody] AnularPagoEstudianteCommand command)
        {
            bool exito = await _mediator.Send(command);
            if (!exito)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "No se pudo anular el pago. Verifique que exista un pago activo en el detalle de cuota y que la cuota no se encuentre cerrada."
                });
            }

            return Ok(new { exito, mensaje = "El último abono ha sido anulado y el saldo recalculado." });
        }

        [HttpGet("{cuotaId}/pendientes")]
        public async Task<IActionResult> GetEstudiantesPendientes(int cuotaId)
        {
            var result = await _mediator.Send(new GetEstudiantesPendientesCuotaQuery(cuotaId));
            return Ok(result);
        }

        [HttpPost("exonerar-estudiante")]
        public async Task<IActionResult> ExonerarEstudiante([FromBody] ExonerarCuotaEstudianteCommand command)
        {
            bool exito = await _mediator.Send(command);
            if (!exito)
            {
                return BadRequest(new { exito = false, mensaje = "No se encontró el detalle de cuota especificado o el estado solicitado no es válido." });
            }

            bool esExoneracion = command.NuevoEstado?.Equals("EXONERADO", StringComparison.OrdinalIgnoreCase) == true;
            return Ok(new
            {
                exito,
                mensaje = esExoneracion
                    ? "La cuota del estudiante ha sido exonerada correctamente."
                    : "La exoneración ha sido revertida correctamente."
            });
        }

        [HttpGet("{cuotaId:int}/exonerados")]
        public async Task<IActionResult> GetEstudiantesExonerados(int cuotaId)
        {
            var result = await _mediator.Send(new GetEstudiantesExoneradosCuotaQuery(cuotaId));
            return Ok(result);
        }

        [HttpPost("cambiar-estado")]
        public async Task<IActionResult> CambiarEstado([FromBody] CambiarEstadoCuotaCommand command)
        {
            bool exito = await _mediator.Send(command);
            if (!exito)
            {
                return BadRequest(new { exito = false, mensaje = "No se encontró la cuota especificada o el estado solicitado no es válido." });
            }

            return Ok(new { exito, mensaje = $"La cuota ha cambiado a estado {command.NuevoEstado} exitosamente." });
        }
    }
}
