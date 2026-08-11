using AulaComite.Application.Comite.Commands;
using AulaComite.Application.Comite.Queries;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComiteController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ISasiAuthService _sasiAuthService;

        public ComiteController(IMediator mediator, ISasiAuthService sasiAuthService)
        {
            _mediator = mediator;
            _sasiAuthService = sasiAuthService;
        }

        [HttpGet("aula/{aulaId}")]
        [Authorize(Policy = "GestionEscolar")]
        public async Task<IActionResult> GetPorAula(int aulaId)
        {
            var result = await _mediator.Send(new GetComitePorAulaQuery(aulaId));
            return Ok(result);
        }

        [HttpGet("apoderados-sasi")]
        [Authorize(Policy = "GestionEscolar")]
        public async Task<IActionResult> GetApoderadosSasi()
        {
            var apoderados = await _sasiAuthService.ObtenerApoderadosAsync();
            return Ok(apoderados);
        }

        [HttpPost]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> AsignarIntegrante([FromBody] AsignarComiteCommand command)
        {
            var id = await _mediator.Send(command);
            return Created($"/api/Comite/{id}", new { id, mensaje = "Integrante asignado al comité con éxito." });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> EliminarIntegrante(int id)
        {
            var result = await _mediator.Send(new DeleteComiteCommand(id));
            if (!result) return NotFound(new { mensaje = "No se encontró el registro a remover." });

            return Ok(new { mensaje = "Integrante removido del comité." });
        }
    }
}
