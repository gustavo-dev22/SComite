using AulaComite.Application.Common.Dto;
using AulaComite.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Interfaces
{
    public interface ISasiAuthService
    {
        Task<AuthResultDto> AutenticarAsync(LoginRequestDto request);

        // Canjea el authorization code del flujo SSO de SASI y establece la sesión local.
        Task<AuthResultDto> AutenticarConCodigoSsoAsync(string code, string codeVerifier, string redirectUri);

        // Comprueba que SASI responda (usado por el frontend antes de redirigir al login SSO).
        Task<bool> VerificarDisponibilidadAsync();

        Task<IEnumerable<UsuarioSasiDto>> ObtenerApoderadosAsync();
    }
}
