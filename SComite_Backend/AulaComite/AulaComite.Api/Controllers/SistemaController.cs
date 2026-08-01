using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Sistema.Commands;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SistemaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SistemaController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("reset-database")]
        public async Task<IActionResult> ResetBaseDeDatos([FromBody] ResetBaseDeDatosCommand command)
        {
            try
            {
                var ok = await _mediator.Send(command);
                return Ok(new { success = ok, message = "La base de datos se ha purgado por completo de forma exitosa." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
