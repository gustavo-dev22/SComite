using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Logss.Queries;
using AulaComite.Application.Logss.Commands;
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
            // Si no viene IP, intentamos obtenerla de la solicitud HTTP
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var commandConIp = command with { IP = command.IP ?? clientIp };

            await _mediator.Send(commandConIp);
            return Ok(new { mensaje = "Log de auditoría registrado correctamente." });
        }
    }
}
