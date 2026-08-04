using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Institucion.Commands;
using AulaComite.Application.Institucion.Queries;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InstitucionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InstitucionController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetConfiguracion()
        {
            var result = await _mediator.Send(new GetInstitucionEducativaQuery());
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar([FromBody] GuardarInstitucionEducativaCommand command)
        {
            var usuarioNombre = !string.IsNullOrWhiteSpace(command.UsuarioActualizacion)
                ? command.UsuarioActualizacion
                : (User.Identity?.Name ?? "ADMIN_SASI");

            var commandFinal = command with { UsuarioActualizacion = usuarioNombre };

            var ok = await _mediator.Send(commandFinal);
            var configuracionActual = await _mediator.Send(new GetInstitucionEducativaQuery());

            return Ok(new { exito = ok, mensaje = "Datos de la Institución Educativa guardados correctamente.", fechaActualizacion = configuracionActual?.FechaActualizacion, usuarioActualizacion = usuarioNombre });
        }
    }
}
