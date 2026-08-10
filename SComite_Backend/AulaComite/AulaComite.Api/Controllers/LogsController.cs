using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Logs.Queries;
using AulaComite.Application.Logs.Commands;
using AulaComite.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Administrador")]
    public class LogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin,
            [FromQuery] string? nivel,
            [FromQuery] string? modulo,
            [FromQuery] string? busqueda,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 20)
        {
            var query = new GetLogsQuery(fechaInicio, fechaFin, nivel, modulo, busqueda, pagina, tamanoPagina);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarLog([FromBody] CreateLogCommand command)
        {
            // El usuario e IP se derivan del token JWT y de la petición HTTP (nunca del cuerpo JSON).
            await _mediator.Send(command);
            return Ok(new { mensaje = "Log de auditoría registrado correctamente." });
        }
    }
}
