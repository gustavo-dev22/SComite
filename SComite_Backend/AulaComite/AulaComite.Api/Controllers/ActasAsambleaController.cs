using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.ActasAsamblea.Commands;
using AulaComite.Application.ActasAsamblea.Queries;
using Microsoft.AspNetCore.Authorization;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "GestionEscolar")]
    public class ActasAsambleaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ActasAsambleaController(IMediator mediator) => _mediator = mediator;

        [HttpGet("aula/{aulaId:int}")]
        public async Task<IActionResult> GetPorAula(int aulaId, [FromQuery] int anio)
        {
            var result = await _mediator.Send(new GetActasPorAulaQuery(aulaId, anio));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromBody] GuardarActaCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { id, mensaje = "Acta de asamblea registrada correctamente." });
        }

        [HttpDelete("{id:int}/aula/{aulaId:int}")]
        public async Task<IActionResult> Eliminar(int id, int aulaId)
        {
            var ok = await _mediator.Send(new EliminarActaCommand(id, aulaId));
            if (!ok) return NotFound(new { mensaje = "No se encontró el acta especificada." });
            return Ok(new { mensaje = "Acta eliminada correctamente." });
        }

        [HttpGet("aula/{aulaId:int}/siguiente-numero")]
        public async Task<IActionResult> GetSiguienteNumero(int aulaId, [FromQuery] int anio)
        {
            var siguienteNumero = await _mediator.Send(new GetSiguienteNumeroActaQuery(aulaId, anio));
            return Ok(new { siguienteNumeroActa = siguienteNumero });
        }
    }
}
