using AulaComite.Application.Apoderado.Commands;
using AulaComite.Application.Apoderado.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AccesoApoderado")]
    public class ApoderadoController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ApoderadoController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// Obtiene la lista de hijos asociados al apoderado en sesión para el año lectivo.
        /// </summary>
        [HttpGet("mis-hijos")]
        public async Task<IActionResult> GetMisHijos([FromQuery] int anio)
        {
            var result = await _mediator.Send(new GetHijosApoderadoQuery(anio));
            return Ok(result);
        }

        /// <summary>
        /// Obtiene el cronograma y estado de pagos del hijo seleccionado.
        /// </summary>
        [HttpGet("cuotas-pendientes")]
        public async Task<IActionResult> GetCuotasPendientes([FromQuery] int estudianteId, [FromQuery] int anio)
        {
            var result = await _mediator.Send(new GetCuotasPendientesApoderadoQuery(estudianteId, anio));
            return Ok(result);
        }

        /// <summary>
        /// Obtiene la lista de anuncios del muro para la vista del apoderado
        /// </summary>
        [HttpGet("anuncios-muro")]
        public async Task<IActionResult> GetAnunciosMuro([FromQuery] int estudianteId, [FromQuery] int anio)
        {
            var result = await _mediator.Send(new GetAnunciosMuroApoderadoQuery(estudianteId, anio));
            return Ok(result);
        }

        /// <summary>
        /// Registra que el apoderado ha visualizado el anuncio y suma +1 en las vistas
        /// </summary>
        [HttpPost("marcar-lectura-anuncio")]
        public async Task<IActionResult> RegistrarLecturaAnuncio([FromBody] RegistrarLecturaAnuncioCommand command)
        {
            var registrado = await _mediator.Send(command);
            if (!registrado)
            {
                // 🛡️ El estudiante no es hijo del apoderado autenticado: 403 Forbidden.
                return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = "No tiene permisos para registrar la lectura sobre este estudiante." });
            }

            return Ok(new { exito = true, mensaje = "Lectura del anuncio registrada correctamente." });
        }

        /// <summary>
        /// Obtiene el cronograma de eventos del aula para el estudiante seleccionado
        /// </summary>
        [HttpGet("cronograma-eventos")]
        public async Task<IActionResult> GetCronogramaEventos([FromQuery] int estudianteId, [FromQuery] int anio)
        {
            var result = await _mediator.Send(new GetCronogramaEventosApoderadoQuery(estudianteId, anio));
            return Ok(result);
        }

        /// <summary>
        /// Obtiene el libro de actas de asamblea aprobadas para el estudiante
        /// </summary>
        [HttpGet("actas-aprobadas")]
        public async Task<IActionResult> GetActasAprobadas([FromQuery] int estudianteId, [FromQuery] int anio)
        {
            var result = await _mediator.Send(new GetActasAprobadasApoderadoQuery(estudianteId, anio));
            return Ok(result);
        }
    }
}
