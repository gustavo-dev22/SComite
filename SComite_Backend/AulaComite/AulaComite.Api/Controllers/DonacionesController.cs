using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Donaciones.Commands;
using AulaComite.Application.Donaciones.Queries;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DonacionesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DonacionesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("aula/{aulaId}")]
        public async Task<IActionResult> GetPorAula(int aulaId, [FromQuery] int anio, [FromQuery] int? mes = null)
        {
            var result = await _mediator.Send(new GetDonacionesPorAulaQuery(aulaId, anio, mes));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromBody] GuardarDonacionCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { id, mensaje = "Donación registrada correctamente." });
        }

        [HttpDelete("{id}/aula/{aulaId}")]
        public async Task<IActionResult> Eliminar(int id, int aulaId)
        {
            var ok = await _mediator.Send(new EliminarDonacionCommand(id, aulaId));
            if (!ok) return NotFound(new { mensaje = "No se encontró el registro a eliminar." });
            return Ok(new { mensaje = "Donación eliminada correctamente." });
        }
    }
}
