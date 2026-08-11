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
            return Ok(new { exito, mensaje = "El pago ha sido anulado y marcado como PENDIENTE." });
        }

        [HttpGet("{cuotaId}/pendientes")]
        public async Task<IActionResult> GetEstudiantesPendientes(int cuotaId)
        {
            var result = await _mediator.Send(new GetEstudiantesPendientesCuotaQuery(cuotaId));
            return Ok(result);
        }
    }
}
