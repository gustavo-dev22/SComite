using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Logs.Queries;
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

        // NOTA M16: La escritura de logs (POST /api/logs) se retiró del API pública.
        // El frontend (log.service.ts) solo realiza consultas GET. Los logs de auditoría
        // se registran de forma interna por los handlers de negocio y el middleware de
        // excepciones (a través de ILogRepository), evitando la inundación de logs falsos.
    }
}
