using System.Net;
using System.Text.Json;
using AulaComite.Application.Common.Interfaces;
using FluentValidation;

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
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                    var validationResponse = new
                    {
                        statusCode = context.Response.StatusCode,
                        mensaje = "La solicitud no es válida.",
                        errores = validationException.Errors
                            .Select(e => new { campo = e.PropertyName, mensaje = e.ErrorMessage })
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(validationResponse));
                    return;
                }

                _logger.LogError(ex, "Excepción capturada en {Path}: {Message}", context.Request.Path, ex.Message);

                // 1. Persistir el log de error en SQL Server vía Stored Procedure
                await RegistrarLogErrorEnBdAsync(context, logRepository, ex);

                // 2. Responder al cliente de forma estándar
                await HandleExceptionAsync(context, ex);
            }
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                mensaje = "Ha ocurrido un error interno en el servidor."
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
