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

            // 🛡️ Regla de seguridad: NO confiar a ciegas en X-Forwarded-For.
            // Solo se usa cuando la petición proviene de un proxy inverso de confianza
            // (p.ej. Kestrel detrás de IIS/NGINX), identificado porque la conexión
            // remota NO es un bucle local. De lo contrario, se usa la IP directa,
            // evitando que el cliente pueda falsificar su IP de auditoría.
            var remoteIp = httpContext.Connection.RemoteIpAddress;

            if (remoteIp != null && !IsPrivate(remoteIp))
            {
                string? forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(forwardedFor))
                {
                    string ip = forwardedFor.Split(',')[0].Trim();
                    if (!string.IsNullOrEmpty(ip)) return ip;
                }
            }

            string remote = remoteIp?.ToString() ?? string.Empty;

            // Si estás probando localmente en IPv6 (::1), mapearlo a 127.0.0.1
            if (remote == "::1" || string.IsNullOrEmpty(remote))
            {
                return "127.0.0.1";
            }

            return remote;
        }

        private static bool IsPrivate(System.Net.IPAddress ip)
        {
            if (System.Net.IPAddress.IsLoopback(ip)) return true;

            var bytes = ip.GetAddressBytes();
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                if (bytes[0] == 10) return true;                                // 10.0.0.0/8
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true; // 172.16.0.0/12
                if (bytes[0] == 192 && bytes[1] == 168) return true;            // 192.168.0.0/16
                if (bytes[0] == 169 && bytes[1] == 254) return true;            // 169.254.x.x (link-local)
            }
            return false;
        }
    }
}