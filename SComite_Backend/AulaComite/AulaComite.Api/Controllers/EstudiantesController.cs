using AulaComite.Application.Estudiantes.Commands;
using AulaComite.Application.Estudiantes.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstudiantesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EstudiantesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("aula/{aulaId}")]
        [Authorize(Policy = "GestionEscolar")]
        public async Task<IActionResult> GetPorAula(int aulaId)
        {
            var result = await _mediator.Send(new GetEstudiantesPorAulaQuery(aulaId));
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Crear([FromBody] CreateEstudianteCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { id, mensaje = "Estudiante registrado con éxito." });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] UpdateEstudianteCommand command)
        {
            if (id != command.Id) return BadRequest(new { mensaje = "ID inconsistente." });

            var result = await _mediator.Send(command);
            if (!result) return NotFound(new { mensaje = "No se encontró el estudiante a actualizar." });

            return Ok(new { mensaje = "Datos del estudiante actualizados." });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var result = await _mediator.Send(new DeleteEstudianteCommand(id));
            if (!result) return NotFound(new { mensaje = "No se encontró el estudiante a desactivar." });

            return Ok(new { mensaje = "Estudiante desactivado del padrón." });
        }

        [HttpPost("carga-masiva")]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> CargaMasiva([FromBody] CargaMasivaEstudiantesCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
