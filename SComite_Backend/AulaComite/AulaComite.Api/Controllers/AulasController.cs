using AulaComite.Application.Aulas.Commands;
using AulaComite.Application.Aulas.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AulasController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAulaRepository _aulaRepository;

        public AulasController(IMediator mediator, IAulaRepository aulaRepository)
        {
            _mediator = mediator;
            _aulaRepository = aulaRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAulas([FromQuery] int? periodoId)
        {
            var result = await _mediator.Send(new GetAulasQuery(periodoId));
            return Ok(result);
        }

        [HttpGet("periodos")]
        public async Task<IActionResult> GetPeriodos()
        {
            var result = await _aulaRepository.ObtenerPeriodosAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CrearAula([FromBody] CreateAulaCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { id, mensaje = "Aula registrada correctamente." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarAula(int id, [FromBody] UpdateAulaCommand command)
        {
            if (id != command.Id) return BadRequest(new { mensaje = "El ID no coincide con la petición." });

            var result = await _mediator.Send(command);
            if (!result) return NotFound(new { mensaje = "No se encontró el aula a actualizar." });

            return Ok(new { mensaje = "Aula actualizada correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarAula(int id)
        {
            var result = await _mediator.Send(new DeleteAulaCommand(id));
            if (!result) return NotFound(new { mensaje = "No se encontró el aula a eliminar." });

            return Ok(new { mensaje = "Aula desactivada correctamente." });
        }
    }
}
