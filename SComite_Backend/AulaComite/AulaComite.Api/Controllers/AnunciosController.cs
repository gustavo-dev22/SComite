using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Anuncios.Commands;
using AulaComite.Application.Anuncios.Queries;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnunciosController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AnunciosController(IMediator mediator) => _mediator = mediator;

        [HttpGet("aula/{aulaId:int}")]
        public async Task<IActionResult> GetPorAula(int aulaId, [FromQuery] int anio)
        {
            var result = await _mediator.Send(new GetAnunciosPorAulaQuery(aulaId, anio));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromBody] GuardarAnuncioCommand command)
        {
            // 🚀 Si por alguna razón el usuarioRegistro viene vacío, extraerlo del token JWT / Identity
            var usuarioNombre = !string.IsNullOrWhiteSpace(command.UsuarioRegistro)
                ? command.UsuarioRegistro
                : (User.Identity?.Name ?? "Comité de Aula");

            var commandFinal = command with { UsuarioRegistro = usuarioNombre };

            var id = await _mediator.Send(commandFinal);
            return Ok(new { id, message = "Comunicado publicado correctamente." });
        }

        [HttpDelete("{id:int}/aula/{aulaId:int}")]
        public async Task<IActionResult> Eliminar(int id, int aulaId)
        {
            var ok = await _mediator.Send(new EliminarAnuncioCommand(id, aulaId));
            if (!ok) return NotFound(new { message = "No se encontró el comunicado." });
            return Ok(new { message = "Comunicado eliminado correctamente." });
        }
    }
}
