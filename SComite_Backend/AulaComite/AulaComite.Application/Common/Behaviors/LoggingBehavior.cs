using AulaComite.Application.Common.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AulaComite.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly ILogger<TRequest> _logger;

        public LoggingBehavior(ILogger<TRequest> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            // 🚀 Registra el inicio de la petición sin incluir datos sensibles (claves, tokens, contraseñas)
            _logger.LogInformation("Procesando Solicitud CQRS: {Name} {Request}", requestName, RequestLogSanitizer.Sanitizar(request));

            var response = await next();

            _logger.LogInformation("Solicitud CQRS Completada: {Name}", requestName);

            return response;
        }
    }
}