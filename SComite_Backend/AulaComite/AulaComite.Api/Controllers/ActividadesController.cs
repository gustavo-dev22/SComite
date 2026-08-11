using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Actividades.Commands;
using AulaComite.Application.Actividades.Queries;
using Microsoft.AspNetCore.Authorization;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "GestionEscolar")]
    public class ActividadesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ActividadesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("aula/{aulaId}")]
        public async Task<IActionResult> GetPorAula(int aulaId, [FromQuery] int anio)
        {
            var result = await _mediator.Send(new GetActividadesPorAulaQuery(aulaId, anio));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromBody] GuardarActividadCommand command)
        {
            var id = await _mediator.Send(command);
            return Created($"/api/Actividades/{id}", new { id, mensaje = "Actividad guardada correctamente." });
        }

        [HttpDelete("{id}/aula/{aulaId}")]
        public async Task<IActionResult> Eliminar(int id, int aulaId)
        {
            var ok = await _mediator.Send(new EliminarActividadCommand(id, aulaId));
            if (!ok) return NotFound(new { mensaje = "No se encontró la actividad a eliminar." });
            return Ok(new { mensaje = "Actividad eliminada correctamente." });
        }
    }
}
