using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AulaComite.Infrastructure.Services
{
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string ObtenerUsuarioActual()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return "Sistema";

            // Identidad proviene del ClaimsPrincipal autenticado por JwtBearer,
            // ya validado por .NET (firma, issuer, audience y expiración).
            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated != true) return "Anónimo";

            var value = user.FindFirst("nombreCompleto")?.Value
                     ?? user.FindFirst(ClaimTypes.Name)?.Value
                     ?? user.FindFirst(ClaimTypes.Email)?.Value
                     ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return string.IsNullOrEmpty(value) ? "Anónimo" : value;
        }

        public string ObtenerIpCliente()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return "127.0.0.1";

            string? forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();

            // Si estás probando localmente en IPv6 (::1), mapearlo a 127.0.0.1
            if (remoteIp == "::1" || string.IsNullOrEmpty(remoteIp))
            {
                return "127.0.0.1";
            }

            return remoteIp;
        }
    }
}