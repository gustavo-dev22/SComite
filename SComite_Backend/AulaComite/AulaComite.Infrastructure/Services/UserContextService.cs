using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Security.Claims;

namespace AulaComite.Infrastructure.Services
{
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JwtSettings _jwtSettings;

        public UserContextService(IHttpContextAccessor httpContextAccessor, IOptions<JwtSettings> jwtSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _jwtSettings = jwtSettings.Value;
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

            // 🛡️ T4.6 Hardening: SOLO se confía en los claims de ROL estándar
            // (ClaimTypes.Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            // emitidos por el token JWT propio firmado por JwtTokenService. Se evita IsInRole()
            // (que depende del RoleClaimType configurado y podría considerar claims de rol de
            // otras fuentes) y se exige que el claim declare el issuer del token (o el default
            // "LOCAL AUTHORITY" de JwtBearer), descartando roles de emisores externos.
            foreach (var claim in user.FindAll(ClaimTypes.Role))
            {
                if (!string.IsNullOrWhiteSpace(claim.Issuer)
                    && !string.Equals(claim.Issuer, _jwtSettings.Issuer, StringComparison.Ordinal)
                    && !string.Equals(claim.Issuer, "LOCAL AUTHORITY", StringComparison.Ordinal))
                {
                    continue;
                }

                if (claim.Value == "Administrador" || claim.Value == "Administrador Global")
                    return true;
            }

            return false;
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