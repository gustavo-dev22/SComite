using System.Text.Json;
using AulaComite.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AulaComite.Api.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ILogRepository logRepository)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (ex is ValidationException validationException)
                {
                    await EscribirProblemDetailsAsync(
                        context,
                        StatusCodes.Status400BadRequest,
                        "La solicitud no es válida.",
                        validationException.Message,
                        extensions: new Dictionary<string, object?>
                        {
                            ["errores"] = validationException.Errors
                                .Select(e => new { campo = e.PropertyName, mensaje = e.ErrorMessage })
                        });
                    return;
                }

                if (ex is UnauthorizedAccessException)
                {
                    await EscribirProblemDetailsAsync(
                        context,
                        StatusCodes.Status403Forbidden,
                        "Acceso denegado.",
                        ex.Message ?? "No tiene permisos para realizar esta operación.");
                    return;
                }

                // 🛡️ T4: Los handlers lanzan KeyNotFoundException cuando el recurso NO existe
                // (después de verificar la existencia PRIMERO), distinguiéndolo del 403 que se
                // reserva para recursos existentes a los que el usuario no tiene acceso.
                if (ex is KeyNotFoundException)
                {
                    await EscribirProblemDetailsAsync(
                        context,
                        StatusCodes.Status404NotFound,
                        "No se encontró el recurso solicitado.",
                        ex.Message ?? "No se encontró el recurso solicitado.");
                    return;
                }

                // 🛡️ T2.0: Los errores de negocio lanzados desde Stored Procedures (THROW
                // 50000-59999) son errores esperados de validación del dominio, no fallas
                // del servidor. Se devuelven como 400 Bad Request sin registrarlos como ERROR.
                if (ex is SqlException sqlEx && sqlEx.Number >= 50000 && sqlEx.Number < 60000)
                {
                    await EscribirProblemDetailsAsync(
                        context,
                        StatusCodes.Status400BadRequest,
                        "La operación no cumple con las reglas de negocio.",
                        sqlEx.Message ?? "La operación no cumple con las reglas de negocio.");
                    return;
                }

                _logger.LogError(ex, "Excepción capturada en {Path}: {Message}", context.Request.Path, ex.Message);

                // 1. Persistir el log de error en SQL Server vía Stored Procedure
                await RegistrarLogErrorEnBdAsync(context, logRepository, ex);

                // 2. Responder al cliente de forma estándar (sin exponer trazas internas)
                await EscribirProblemDetailsAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Ha ocurrido un error interno en el servidor.",
                    "Ha ocurrido un error interno en el servidor.");
            }
        }

        /// <summary>
        /// 🛡️ T4.6: Escribe la respuesta de error como <see cref="ProblemDetails"/> (RFC 7807)
        /// con <c>status</c>, <c>title</c>, <c>detail</c>, <c>instance</c> y <c>traceId</c>.
        /// Nunca expone stack traces ni detalles internos al cliente.
        /// </summary>
        private static async Task EscribirProblemDetailsAsync(
            HttpContext context,
            int status,
            string title,
            string detail,
            IReadOnlyDictionary<string, object?>? extensions = null)
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status;

            var problemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path.HasValue ? context.Request.Path.Value : null,
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            };

            if (extensions != null)
            {
                foreach (var kvp in extensions)
                {
                    problemDetails.Extensions[kvp.Key] = kvp.Value;
                }
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }

        private static async Task RegistrarLogErrorEnBdAsync(HttpContext context, ILogRepository logRepository, Exception ex)
        {
            try
            {
                string ruta = context.Request.Path.Value ?? "/";
                string metodo = context.Request.Method;

                string modulo = "GENERAL";
                if (ruta.Contains("Periodos", StringComparison.OrdinalIgnoreCase)) modulo = "PERIODOS";
                else if (ruta.Contains("Aulas", StringComparison.OrdinalIgnoreCase)) modulo = "AULAS";
                else if (ruta.Contains("Estudiantes", StringComparison.OrdinalIgnoreCase)) modulo = "ESTUDIANTES";
                else if (ruta.Contains("Comite", StringComparison.OrdinalIgnoreCase)) modulo = "COMITE";
                else if (ruta.Contains("Auth", StringComparison.OrdinalIgnoreCase)) modulo = "AUTH";

                await logRepository.RegistrarAsync(
                    nivel: "ERROR",
                    modulo: modulo,
                    accion: $"{metodo} {ruta}",
                    mensaje: ex.Message,
                    exception: ex.ToString()
                // 🚀 Ya no es necesario pasar usuario e IP; se capturan automáticamente de la petición HTTP
                );
            }
            catch
            {
                // Evitar que falle el middleware si la BD está inaccesible
            }
        }
    }
}
