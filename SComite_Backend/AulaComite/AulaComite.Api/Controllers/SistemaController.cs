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
                return Ok(new { success = ok, message = "Se ha generado el backup pre-purga y la base de datos se ha limpiado por completo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("descargar-backup")]
        public async Task<IActionResult> DescargarBackup()
        {
            var fileBytes = await _mediator.Send(new GenerarBackupManualCommand());
            var fileName = $"Backup_AulaComite_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            return File(fileBytes, "application/sql", fileName);
        }
    }
}
