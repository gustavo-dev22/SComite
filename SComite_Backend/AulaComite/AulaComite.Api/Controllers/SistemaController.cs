using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Sistema.Commands;
using AulaComite.Domain.Common;
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
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> ResetBaseDeDatos([FromBody] ResetBaseDeDatosCommand command)
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            var resultado = await _mediator.Send(command);
            if (!resultado.Exito)
            {
                return resultado.EsErrorDeAutorizacion
                    ? StatusCode(StatusCodes.Status403Forbidden, new { mensaje = resultado.Mensaje })
                    : BadRequest(new { mensaje = resultado.Mensaje });
            }

            return Ok(new { exito = true, mensaje = resultado.Mensaje });
        }

        [HttpGet("descargar-backup")]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> DescargarBackup()
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            var fileBytes = await _mediator.Send(new GenerarBackupManualCommand());
            var fileName = $"Backup_AulaComite_{DateTimeHelper.ObtenerHoraPeru():yyyyMMdd_HHmmss}.sql";
            return File(fileBytes, "application/sql", fileName);
        }
    }
}
