using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Auditoria.Queries;
using Microsoft.AspNetCore.Authorization;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Administrador")]
    public class AuditoriaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditoriaController(IMediator mediator) => _mediator = mediator;

        [HttpGet("resumen-cajas")]
        public async Task<IActionResult> GetResumenGeneralCajas([FromQuery] int anio, [FromQuery] string? nivel)
        {
            var result = await _mediator.Send(new GetResumenGeneralCajasQuery(anio, nivel));
            return Ok(result);
        }
    }
}
