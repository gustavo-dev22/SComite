using AulaComite.Application.Common.Dto;
using AulaComite.Application.Common.Exceptions;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AulaComite.Infrastructure.Services
{
    public class SasiAuthService : ISasiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SasiAuthService> _logger;
        private readonly int _sistemaIdTarget;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ISasiTokenStore _sasiTokenStore;
        private readonly IUserContextService _userContextService;

        public SasiAuthService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SasiAuthService> logger,
            IJwtTokenService jwtTokenService,
            ISasiTokenStore sasiTokenStore,
            IUserContextService userContextService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jwtTokenService = jwtTokenService;
            _sasiTokenStore = sasiTokenStore;
            _userContextService = userContextService;
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

                // 🛡️ SASI-DOWN/FIX: El catálogo de apoderados vive en un endpoint de SASI
                // protegido por JWT ([Authorize]). Guardamos el token emitido por SASI en el
                // login para autenticar (Bearer) las llamadas backend-a-backend posteriores.
                if (!string.IsNullOrWhiteSpace(sasiResult.Token) && sasiResult.Usuario != null)
                {
                    _sasiTokenStore.Guardar(sasiResult.Usuario.Id, sasiResult.Token);
                }

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
                // 🛡️ SASI-DOWN/FIX: El endpoint de SASI está protegido con JWT. Se envía el
                // token emitido por SASI en el login (guardado por usuario) como Bearer.
                var usuarioId = _userContextService.ObtenerUsuarioId();
                var tokenSasi = !string.IsNullOrWhiteSpace(usuarioId)
                    ? _sasiTokenStore.Obtener(usuarioId)
                    : null;

                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"sistemas/por-sistema-y-rol?sistemaId={_sistemaIdTarget}&rolNombre=Apoderado");

                if (!string.IsNullOrWhiteSpace(tokenSasi))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSasi);
                }

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    // 🛡️ T2.5/SASI-DOWN: si SASI responde 401 (token ausente/expirado) o 5xx,
                    // no se devuelve lista vacía silenciosamente: se notifica de forma explícita.
                    _logger.LogWarning("SASI respondió {Status} al obtener apoderados.", (int)response.StatusCode);
                    throw new SasiNoDisponibleException(
                        "El servicio de autenticación (SASI) no está disponible en este momento. " +
                        "No se pudieron cargar los apoderados. Intente nuevamente en unos minutos.");
                }

                var sasiResult = await response.Content.ReadFromJsonAsync<SasiResponseDto<List<UsuarioSasiDto>>>();

                return sasiResult?.Datos ?? new List<UsuarioSasiDto>();
            }
            catch (SasiNoDisponibleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 🛡️ T2.5/SASI-DOWN: NO se devuelve lista vacía silenciosamente. Si SASI está
                // caído, la operación que depende de su catálogo (registro de estudiante,
                // asignación de comité, carga masiva) debe notificarlo de forma explícita y
                // amigable, evitando que el usuario confunda "sin apoderados" con "SASI caído".
                _logger.LogWarning(ex, "SASI no disponible al obtener apoderados: {Message}", ex.Message);

                throw new SasiNoDisponibleException(
                    "El servicio de autenticación (SASI) no está disponible en este momento. " +
                    "No se pudieron cargar los apoderados. Intente nuevamente en unos minutos.",
                    ex);
            }
        }
    }
}
