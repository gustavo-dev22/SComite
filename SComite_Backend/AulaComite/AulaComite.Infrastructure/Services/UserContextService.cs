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

        public string? ObtenerUsuarioId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated != true) return null;

            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value;
        }

        public bool EsAdministradorGlobal()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return false;

            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated != true) return false;

            return user.IsInRole("Administrador") || user.IsInRole("Administrador Global");
        }

        public string ObtenerIpCliente()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return "127.0.0.1";

            // 🛡️ Regla de seguridad: NO se lee X-Forwarded-For directamente (spoofeable).
            // Se confía únicamente en la IP ya calculada por UseForwardedHeaders, que solo
            // procesa la cabecera cuando el peer directo es un proxy de confianza (bucle
            // local / red privada de IIS o rango publicado de Cloudflare), configurado en
            // Program.cs. Un cliente de Internet no puede falsificar su IP de auditoría.
            var remote = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            // Si estás probando localmente en IPv6 (::1), mapearlo a 127.0.0.1
            if (remote == "::1" || string.IsNullOrEmpty(remote))
            {
                return "127.0.0.1";
            }

            return remote;
        }
    }
}