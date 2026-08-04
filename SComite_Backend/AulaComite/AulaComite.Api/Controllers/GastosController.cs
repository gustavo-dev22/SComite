using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Gastos.Commands;
using AulaComite.Application.Gastos.Queries;
using Microsoft.AspNetCore.Authorization;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ManejoFinanciero")]
    public class GastosController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GastosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("aula/{aulaId:int}")]
        public async Task<IActionResult> GetPorAula(int aulaId)
        {
            var result = await _mediator.Send(new GetGastosPorAulaQuery(aulaId));
            return Ok(result);
        }

        [HttpGet("aula/{aulaId:int}/resumen-caja")]
        public async Task<IActionResult> GetResumenCaja(int aulaId)
        {
            var result = await _mediator.Send(new GetResumenCajaQuery(aulaId));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CreateGastoCommand command)
        {
            int id = await _mediator.Send(command);
            return Ok(new { id, mensaje = "Gasto registrado correctamente." });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            bool exito = await _mediator.Send(new DeleteGastoCommand(id));
            if (!exito) return NotFound(new { mensaje = "No se encontró el gasto a eliminar." });

            return Ok(new { exito, mensaje = "Gasto eliminado de la caja." });
        }

        [HttpGet("aula/{aulaId:int}/balance-mensual")]
        public async Task<IActionResult> GetBalanceMensual(int aulaId, [FromQuery] int anioLectivo, [FromQuery] int? mes)
        {
            var result = await _mediator.Send(new GetBalanceMensualCajaQuery(aulaId, anioLectivo, mes));
            return Ok(result);
        }
    }
}
