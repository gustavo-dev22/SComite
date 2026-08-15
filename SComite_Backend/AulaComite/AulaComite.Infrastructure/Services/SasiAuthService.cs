using AulaComite.Application.Common.Dto;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace AulaComite.Infrastructure.Services
{
    public class SasiAuthService : ISasiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SasiAuthService> _logger;
        private readonly int _sistemaIdTarget;
        private readonly IJwtTokenService _jwtTokenService;

        public SasiAuthService(HttpClient httpClient, IConfiguration configuration, ILogger<SasiAuthService> logger, IJwtTokenService jwtTokenService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jwtTokenService = jwtTokenService;
            var configVal = configuration["SasiSettings:SistemaId"]
                ?? throw new InvalidOperationException("SasiSettings:SistemaId no está configurado.");
            _sistemaIdTarget = int.Parse(configVal);
        }

        public async Task<AuthResultDto> AutenticarAsync(LoginRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("Auth/login", request);

                if (!response.IsSuccessStatusCode)
                {
                    // 🛡️ M3: Respuesta GENÉRICA para no revelar si la cuenta existe o no.
                    return new AuthResultDto { Exito = false, Bloqueado = false, Mensaje = "Usuario o contraseña incorrectos." };
                }

                var sasiResult = await response.Content.ReadFromJsonAsync<SasiLoginResponse>();

                if (sasiResult == null)
                {
                    return new AuthResultDto { Exito = false, Bloqueado = false, Mensaje = "No se pudo procesar la respuesta de autenticación. Inténtelo de nuevo." };
                }

                // 🛡️ M4: Reflejar el estado de BLOQUEO de la cuenta de forma explícita,
                // sin ignorarlo en el flujo de login.
                if (sasiResult.Bloqueado)
                {
                    return new AuthResultDto
                    {
                        Exito = false,
                        Bloqueado = true,
                        Mensaje = "Su cuenta se encuentra bloqueada en el sistema. Contacte al administrador."
                    };
                }

                if (!sasiResult.Success)
                {
                    // 🛡️ M3: Respuesta GENÉRICA ante credenciales incorrectas.
                    return new AuthResultDto { Exito = false, Bloqueado = false, Mensaje = "Usuario o contraseña incorrectos." };
                }

                // 🚀 VALIDACIÓN CRÍTICA: Verificar si tiene acceso al Sistema de Comité de Aula
                var sistemaComite = sasiResult.Usuario?.Sistemas
                    .FirstOrDefault(s => (s.Id == _sistemaIdTarget) && s.Activo);

                if (sistemaComite == null)
                {
                    return new AuthResultDto
                    {
                        Exito = false,
                        Bloqueado = false,
                        Mensaje = "Acceso denegado: Tu usuario no tiene asignado el rol/sistema 'Comité de Aula' en SASI."
                    };
                }

                if (sasiResult.Usuario == null)
                {
                    return new AuthResultDto { Exito = false, Bloqueado = false, Mensaje = "Respuesta inválida de SASI." };
                }

                // Emitir un JWT propio de la aplicación, firmado con la clave local
                // (JwtSettings), para que los endpoints [Authorize] lo acepten.
                var tokenLocal = _jwtTokenService.GenerarToken(sasiResult.Usuario, sistemaComite);

                return new AuthResultDto
                {
                    Exito = true,
                    Bloqueado = sasiResult.Bloqueado,
                    Token = tokenLocal,
                    NombreUsuario = sasiResult.Usuario?.NombreCompleto ?? string.Empty,
                    Email = sasiResult.Usuario?.Email ?? string.Empty,
                    SistemaComite = sistemaComite
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al conectar con el servidor de autenticación SASI: {Message}", ex.Message);
                return new AuthResultDto { Exito = false, Bloqueado = false, Mensaje = "Error al conectar con el servidor de autenticación. Inténtelo de nuevo." };
            }
        }

        public async Task<IEnumerable<UsuarioSasiDto>> ObtenerApoderadosAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<SasiResponseDto<List<UsuarioSasiDto>>>(
                    $"sistemas/por-sistema-y-rol?sistemaId={_sistemaIdTarget}&rolNombre=Apoderado");

                return response?.Datos ?? new List<UsuarioSasiDto>();
            }
            catch
            {
                // Retornar lista vacía si la API de SASI no está disponible
                return new List<UsuarioSasiDto>();
            }
        }
    }
}
