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

        public SistemaController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("reset-database")]
        [Authorize(Policy = "Administrador")]
        public async Task<IActionResult> ResetBaseDeDatos([FromBody] ResetBaseDeDatosCommand command)
        {
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
            var fileBytes = await _mediator.Send(new GenerarBackupManualCommand());
            var fileName = $"Backup_AulaComite_{DateTimeHelper.ObtenerHoraPeru():yyyyMMdd_HHmmss}.sql";
            return File(fileBytes, "application/sql", fileName);
        }
    }
}
