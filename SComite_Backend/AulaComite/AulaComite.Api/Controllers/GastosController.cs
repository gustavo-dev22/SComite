using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Gastos.Commands;
using AulaComite.Application.Gastos.Queries;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
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
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<GastosController> _logger;

        public GastosController(IMediator mediator, IFileStorageService fileStorageService, ILogger<GastosController> logger)
        {
            _mediator = mediator;
            _fileStorageService = fileStorageService;
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
            return Created($"/api/Gastos/{id}", new { id, mensaje = "Gasto registrado correctamente." });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarGasto(int id, [FromBody] UpdateGastoCommand command)
        {
            if (id != command.Id)
                return BadRequest(new { mensaje = "El ID enviado en la ruta no coincide con el cuerpo de la solicitud." });

            // 🛡️ El handler distingue: recurso inexistente -> false (404 aquí) y recurso de un
            // Aula no asignada -> UnauthorizedAccessException (403 vía middleware). La excepción
            // NO se convierte a 400: se deja propagar para que el middleware responda 403.
            var exito = await _mediator.Send(command);
            if (!exito)
                return NotFound(new { mensaje = "El gasto especificado no existe o no se pudo actualizar." });

            return Ok(new { exito = true, mensaje = "Gasto modificado correctamente." });
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
                // 🚀 Streaming directo: se transmite el IFormFile hacia el servicio de
                // almacenamiento sin cargar el buffer completo en arreglos byte[].
                using var stream = archivo.OpenReadStream();

                // 🛡️ Validación de tamaño máximo (5 MB), tipo MIME y FORMATO REAL (magic bytes):
                // un archivo renombrado a .pdf/.jpg se rechaza porque su contenido no coincide.
                ComprobanteFileValidator.Validar(archivo.ContentType, archivo.FileName, archivo.Length, stream);

                var command = new SubirComprobanteGastoCommand(stream, archivo.FileName);
                var urlComprobante = await _mediator.Send(command);

                return Ok(new { urlComprobante });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Comprobante rechazado: {Message}", ex.Message);
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // 🛡️ Endpoint protegido (hereda [Authorize(Policy="ManejoFinanciero")]) para la
        // descarga/visualización de comprobantes financieros. Los archivos locales viven
        // fuera de wwwroot y los de Cloudinary se suben con acceso authenticated.
        [HttpGet("comprobante")]
        public async Task<IActionResult> VerComprobante([FromQuery] string archivo)
        {
            if (string.IsNullOrWhiteSpace(archivo))
                return BadRequest(new { mensaje = "No se especificó el comprobante a mostrar." });

            var descriptor = await _fileStorageService.ObtenerComprobanteAsync(archivo, HttpContext.RequestAborted);
            if (descriptor == null)
                return NotFound(new { mensaje = "No se encontró el comprobante solicitado." });

            return File(descriptor.Contenido, descriptor.TipoContenido ?? "application/octet-stream");
        }
    }
}
