using AulaComite.Application.Aulas.Commands;
using AulaComite.Application.Aulas.Dtos;
using AulaComite.Application.Aulas.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Policy = "GestionEscolar")]
        public async Task<IActionResult> GetAulas([FromQuery] int? periodoId)
        {
            var result = await _mediator.Send(new GetAulasQuery(periodoId));
            return Ok(result);
        }

        // 🛡️ Aulas del usuario logueado: el comité/apoderado solo ve sus aulas;
        // el administrador ve todas. Lo usa el frontend en los selects de "Aula - Sección".
        [HttpGet("mis-aulas")]
        [Authorize]
        public async Task<IActionResult> GetMisAulas([FromQuery] int? periodoId)
        {
            var result = await _mediator.Send(new GetMisAulasQuery(periodoId));
            return Ok(result);
        }

        [HttpGet("periodos")]
        [Authorize]
        public async Task<IActionResult> GetPeriodos()
        {
            var periodos = await _aulaRepository.ObtenerPeriodosAsync();

            var result = periodos.Select(p => new PeriodoLectivoDto
            {
                Id = p.Id,
                Anio = p.Anio,
                Nombre = p.Nombre,
                EsActivo = p.EsActivo,
                FechaInicio = p.FechaInicio,
                FechaFin = p.FechaFin
            });

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> CrearAula([FromBody] CreateAulaCommand command)
        {
            var id = await _mediator.Send(command);
            return Created($"/api/Aulas/{id}", new { id, mensaje = "Aula registrada correctamente." });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> ActualizarAula(int id, [FromBody] UpdateAulaCommand command)
        {
            if (id != command.Id) return BadRequest(new { mensaje = "El ID no coincide con la petición." });

            var result = await _mediator.Send(command);
            if (!result) return NotFound(new { mensaje = "No se encontró el aula a actualizar." });

            return Ok(new { mensaje = "Aula actualizada correctamente." });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> EliminarAula(int id)
        {
            var result = await _mediator.Send(new DeleteAulaCommand(id));
            if (!result) return NotFound(new { mensaje = "No se encontró el aula a eliminar." });

            return Ok(new { mensaje = "Aula desactivada correctamente." });
        }
    }
}
