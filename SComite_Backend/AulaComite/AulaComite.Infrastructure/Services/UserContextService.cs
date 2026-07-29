using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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

            // 🔍 DEBUG 1: Verificar si ClaimsPrincipal detectó la identidad
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                var claimName = httpContext.User.FindFirst("nombreCompleto")?.Value
                             ?? httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                             ?? httpContext.User.FindFirst(ClaimTypes.Email)?.Value
                             ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(claimName)) return claimName;
            }

            // 🚀 FALLBACK / DECODIFICADOR MANUAL DE TOKEN SASI:
            // Si .NET no ha autenticado la request vía middleware, leemos manualmente el Bearer Token
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    var handler = new JwtSecurityTokenHandler();

                    if (handler.CanReadToken(token))
                    {
                        var jwtToken = handler.ReadJwtToken(token);

                        // Extraer claims típicas enviadas por SASI
                        var nombreCompleto = jwtToken.Claims.FirstOrDefault(c => c.Type == "nombreCompleto")?.Value
                                          ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value
                                          ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value
                                          ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                        if (!string.IsNullOrEmpty(nombreCompleto))
                        {
                            return nombreCompleto;
                        }
                    }
                }
                catch
                {
                    // Si el token no es válido o expiró, continúa al valor por defecto
                }
            }

            return "Anónimo";
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
