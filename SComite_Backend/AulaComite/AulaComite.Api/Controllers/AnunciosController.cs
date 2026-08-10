using AulaComite.Application.Anuncios.Commands;
using AulaComite.Application.Anuncios.Queries;
using AulaComite.Application.Comite.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "GestionEscolar")]
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
            var id = await _mediator.Send(command);
            return Ok(new { id, mensaje = "Comunicado publicado correctamente." });
        }

        [HttpDelete("{id:int}/aula/{aulaId:int}")]
        public async Task<IActionResult> Eliminar(int id, int aulaId)
        {
            var ok = await _mediator.Send(new EliminarAnuncioCommand(id, aulaId));
            if (!ok) return NotFound(new { mensaje = "No se encontró el comunicado." });
            return Ok(new { mensaje = "Comunicado eliminado correctamente." });
        }

        [HttpGet("auditoria-vistas/{anuncioId}")]
        public async Task<IActionResult> GetAuditoriaVistas(int anuncioId)
        {
            var result = await _mediator.Send(new GetAuditoriaLecturasAnuncioQuery(anuncioId));
            return Ok(result);
        }
    }
}
