using Microsoft.AspNetCore.Authorization;
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
        private readonly IWebHostEnvironment _env;

        public SistemaController(IMediator mediator, IWebHostEnvironment env)
        {
            _mediator = mediator;
            _env = env;
        }

        [HttpPost("reset-database")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ResetBaseDeDatos([FromBody] ResetBaseDeDatosCommand command)
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                var ok = await _mediator.Send(command);
                return Ok(new { exito = ok, mensaje = "Se ha generado el backup pre-purga y la base de datos se ha limpiado por completo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("descargar-backup")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DescargarBackup()
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            var fileBytes = await _mediator.Send(new GenerarBackupManualCommand());
            var fileName = $"Backup_AulaComite_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            return File(fileBytes, "application/sql", fileName);
        }
    }
}
