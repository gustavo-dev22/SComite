using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Gastos.Commands;
using AulaComite.Application.Gastos.Queries;
using Microsoft.AspNetCore.Authorization;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ManejoFinanciero")]
    public class GastosController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GastosController> _logger;

        public GastosController(IMediator mediator, ILogger<GastosController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("aula/{aulaId:int}")]
        public async Task<IActionResult> GetPorAula(int aulaId)
        {
            var result = await _mediator.Send(new GetGastosPorAulaQuery(aulaId));
            return Ok(result);
        }

        [HttpGet("aula/{aulaId:int}/resumen-caja")]
        public async Task<IActionResult> GetResumenCaja(int aulaId)
        {
            var result = await _mediator.Send(new GetResumenCajaQuery(aulaId));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CreateGastoCommand command)
        {
            int id = await _mediator.Send(command);
            return Ok(new { id, mensaje = "Gasto registrado correctamente." });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarGasto(int id, [FromBody] UpdateGastoCommand command)
        {
            if (id != command.Id)
                return BadRequest(new { mensaje = "El ID enviado en la ruta no coincide con el cuerpo de la solicitud." });

            try
            {
                var exito = await _mediator.Send(command);
                if (!exito)
                    return NotFound(new { mensaje = "El gasto especificado no existe o no se pudo actualizar." });

                return Ok(new { exito = true, mensaje = "Gasto modificado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el gasto {GastoId}.", id);
                return BadRequest(new { mensaje = "Ocurrió un error al procesar la solicitud de gastos. Inténtalo de nuevo." });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            bool exito = await _mediator.Send(new DeleteGastoCommand(id));
            if (!exito) return NotFound(new { mensaje = "No se encontró el gasto a eliminar." });

            return Ok(new { exito, mensaje = "Gasto eliminado de la caja." });
        }

        [HttpGet("aula/{aulaId:int}/balance-mensual")]
        public async Task<IActionResult> GetBalanceMensual(int aulaId, [FromQuery] int anioLectivo, [FromQuery] int? mes)
        {
            var result = await _mediator.Send(new GetBalanceMensualCajaQuery(aulaId, anioLectivo, mes));
            return Ok(result);
        }

        [HttpPost("subir-comprobante")]
        public async Task<IActionResult> SubirComprobante(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { mensaje = "No se ha seleccionado ningún archivo." });

            try
            {
                // 🚀 Leemos el archivo a un memory stream y obtenemos el arreglo de bytes
                using var memoryStream = new MemoryStream();
                await archivo.CopyToAsync(memoryStream);
                var bytesArchivo = memoryStream.ToArray();

                var command = new SubirComprobanteGastoCommand(bytesArchivo, archivo.FileName);
                var urlComprobante = await _mediator.Send(command);

                return Ok(new { urlComprobante });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Comprobante rechazado: {Message}", ex.Message);
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
