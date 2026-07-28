using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

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

            // 🚀 Registra automáticamente el inicio de CUALQUIER comando o consulta
            _logger.LogInformation("Procesando Solicitud CQRS: {Name} {@Request}", requestName, request);

            var response = await next();

            // 🚀 Registra la finalización
            _logger.LogInformation("Solicitud CQRS Completada: {Name}", requestName);

            return response;
        }
    }
}
