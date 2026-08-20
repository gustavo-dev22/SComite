using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AulaComite.Application.Common.Dto;
using AulaComite.Application.Common.Exceptions;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        // 🛡️ SASI rota el refresh token de forma atómica (de un solo uso). Un lock por
        // usuario evita que dos consultas concurrentes refresquen a la vez con el mismo
        // refresh token y una de ellas falle (401).
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locksRefresco = new(StringComparer.OrdinalIgnoreCase);

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
                    // Si SASI adjunta un detalle (p. ej. bloqueo/inactivo) se propaga.
                    var sasiError = await LeerRespuestaErrorAsync(response);
                    if (sasiError != null && !string.IsNullOrWhiteSpace(sasiError.Message))
                    {
                        return new AuthResultDto
                        {
                            Exito = false,
                            Bloqueado = sasiError.Bloqueado,
                            Inactivo = sasiError.Inactivo,
                            Mensaje = sasiError.Message
                        };
                    }

                    return new AuthResultDto { Exito = false, Bloqueado = false, Mensaje = "Usuario o contraseña incorrectos." };
                }

                var sasiResult = await response.Content.ReadFromJsonAsync<SasiLoginResponse>();

                if (sasiResult == null)
                {
                    return new AuthResultDto { Exito = false, Bloqueado = false, Mensaje = "No se pudo procesar la respuesta de autenticación. Inténtelo de nuevo." };
                }

                if (!sasiResult.Success)
                {
                    // 🛡️ M4: El estado de la cuenta (bloqueado/inactivo) se refleja de
                    // forma explícita usando el mensaje que entrega SASI (fuente única
                    // de verdad) para que todos los sistemas integrados muestren el
                    // mismo texto según el estado del usuario.
                    if (sasiResult.Bloqueado)
                    {
                        return new AuthResultDto
                        {
                            Exito = false,
                            Bloqueado = true,
                            Mensaje = sasiResult.Message
                                ?? "Su cuenta se encuentra bloqueada temporalmente por intentos fallidos de inicio de sesión. Contacte al administrador del sistema."
                        };
                    }

                    if (sasiResult.Inactivo)
                    {
                        return new AuthResultDto
                        {
                            Exito = false,
                            Inactivo = true,
                            Mensaje = sasiResult.Message
                                ?? "Su usuario se encuentra inactivo en el sistema. Contacte al administrador para restablecer el acceso."
                        };
                    }

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

                // 🛡️ ROLES CON TOGGLE ACTIVADO: SASI envía todos los roles asignados
                // (activos e inactivos). Los roles desactivados (activo=false) no deben
                // entregarse al frontend ni emitirse como claims del JWT local.
                sistemaComite.Roles = sistemaComite.Roles.Where(r => r.Activo).ToList();

                if (sistemaComite.Roles.Count == 0)
                {
                    return new AuthResultDto
                    {
                        Exito = false,
                        Bloqueado = false,
                        Mensaje = "Acceso denegado: Tu usuario no tiene un rol activo en el sistema 'Comité de Aula' en SASI."
                    };
                }

                // Emitir un JWT propio de la aplicación, firmado con la clave local
                // (JwtSettings), para que los endpoints [Authorize] lo acepten.
                var tokenLocal = _jwtTokenService.GenerarToken(sasiResult.Usuario, sistemaComite);

                // 🛡️ SASI-DOWN/FIX: El catálogo de apoderados vive en un endpoint de SASI
                // protegido por JWT ([Authorize]). Guardamos el token emitido por SASI en el
                // login (y su refresh token) para autenticar (Bearer) y renovar las llamadas
                // backend-a-backend posteriores sin obligar al usuario a volver a iniciar sesión.
                if (sasiResult.Usuario != null && !string.IsNullOrWhiteSpace(sasiResult.Token))
                {
                    _sasiTokenStore.Guardar(sasiResult.Usuario.Id, sasiResult.Token, sasiResult.RefreshToken);
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

        private static async Task<SasiLoginResponse?> LeerRespuestaErrorAsync(HttpResponseMessage response)
        {
            try
            {
                if (response.Content == null) return null;
                return await response.Content.ReadFromJsonAsync<SasiLoginResponse>();
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<UsuarioSasiDto>> ObtenerApoderadosAsync()
        {
            try
            {
                // 🛡️ SASI-DOWN/FIX: El endpoint de SASI está protegido con JWT. Se envía el
                // token emitido por SASI en el login (guardado por usuario) como Bearer.
                var usuarioId = _userContextService.ObtenerUsuarioId();
                var credenciales = !string.IsNullOrWhiteSpace(usuarioId)
                    ? _sasiTokenStore.Obtener(usuarioId)
                    : null;

                var tokenSasi = credenciales?.Token;

                // 🔄 Si el access token expiró (SASI los emite por 8h), renovarlo con el
                // refresh token antes de cada consulta.
                if (!string.IsNullOrWhiteSpace(tokenSasi) && TokenExpirado(tokenSasi))
                {
                    tokenSasi = null;
                }

                if (string.IsNullOrWhiteSpace(tokenSasi)
                    && !string.IsNullOrWhiteSpace(usuarioId)
                    && !string.IsNullOrWhiteSpace(credenciales?.RefreshToken))
                {
                    tokenSasi = await RefrescarTokenAsync(usuarioId, credenciales.RefreshToken);
                }

                var response = await EnviarSolicitudApoderadosAsync(tokenSasi);
                using (response)
                {
                    // 🔄 Reintento automático: si SASI responde 401 (token expirado/inválido),
                    // se refresca el token una vez y se reenvía la consulta.
                    if (response.StatusCode == HttpStatusCode.Unauthorized
                        && !string.IsNullOrWhiteSpace(usuarioId)
                        && !string.IsNullOrWhiteSpace(credenciales?.RefreshToken))
                    {
                        var tokenRenovado = await RefrescarTokenAsync(usuarioId, credenciales.RefreshToken);

                        if (!string.IsNullOrWhiteSpace(tokenRenovado))
                        {
                            using var reintento = await EnviarSolicitudApoderadosAsync(tokenRenovado);
                            return await ProcesarRespuestaApoderadosAsync(reintento);
                        }
                    }

                    return await ProcesarRespuestaApoderadosAsync(response);
                }
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

        private async Task<HttpResponseMessage> EnviarSolicitudApoderadosAsync(string? tokenSasi)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "sistemas/por-sistema-y-rol")
            {
                Content = JsonContent.Create(new { sistemaId = _sistemaIdTarget, rolNombre = "Apoderado" })
            };

            if (!string.IsNullOrWhiteSpace(tokenSasi))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSasi);
            }

            return await _httpClient.SendAsync(request);
        }

        private async Task<IEnumerable<UsuarioSasiDto>> ProcesarRespuestaApoderadosAsync(HttpResponseMessage response)
        {
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

        private async Task<string?> RefrescarTokenAsync(string usuarioId, string refreshToken)
        {
            var semaforo = _locksRefresco.GetOrAdd(usuarioId, _ => new SemaphoreSlim(1, 1));

            if (!await semaforo.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                _logger.LogWarning("Timeout al adquirir lock de refresco de token SASI para {UsuarioId}.", usuarioId);
                return null;
            }

            try
            {
                // 🔄 Rotación atómica de SASI: otro request pudo renovar mientras esperábamos
                // el lock. Si el token actual ya es válido, reutilizarlo.
                var actuales = _sasiTokenStore.Obtener(usuarioId);
                if (actuales != null && !string.IsNullOrWhiteSpace(actuales.Token) && !TokenExpirado(actuales.Token))
                {
                    return actuales.Token;
                }

                var response = await _httpClient.PostAsJsonAsync("Auth/refresh", new { refreshToken });

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SASI respondió {Status} al refrescar token.", (int)response.StatusCode);
                    return null;
                }

                var resultado = await response.Content.ReadFromJsonAsync<SasiRefreshResponse>();

                if (resultado == null || !resultado.Success || string.IsNullOrWhiteSpace(resultado.Token))
                {
                    _logger.LogWarning("SASI no devolvió un token válido al refrescar.");
                    return null;
                }

                _sasiTokenStore.Guardar(usuarioId, resultado.Token, resultado.RefreshToken);

                return resultado.Token;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al refrescar token SASI: {Message}", ex.Message);
                return null;
            }
            finally
            {
                semaforo.Release();
            }
        }

        private static bool TokenExpirado(string token)
        {
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                return jwt.ValidTo <= DateTime.UtcNow.AddMinutes(1);
            }
            catch
            {
                return true;
            }
        }
    }
}
