using AulaComite.Application.Common.Dto;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace AulaComite.Infrastructure.Services
{
    public class SasiAuthService : ISasiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly int _sistemaIdTarget;

        public SasiAuthService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
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
                    return new AuthResultDto { Exito = false, Mensaje = "Credenciales inválidas en SASI." };
                }

                var sasiResult = await response.Content.ReadFromJsonAsync<SasiLoginResponse>();

                if (sasiResult == null || !sasiResult.Success)
                {
                    return new AuthResultDto { Exito = false, Mensaje = "Usuario o contraseña incorrectos." };
                }

                // 🚀 VALIDACIÓN CRÍTICA: Verificar si tiene acceso al Sistema de Comité de Aula
                var sistemaComite = sasiResult.Usuario?.Sistemas
                    .FirstOrDefault(s => (s.Id == _sistemaIdTarget) && s.Activo);

                if (sistemaComite == null)
                {
                    return new AuthResultDto
                    {
                        Exito = false,
                        Mensaje = "Acceso denegado: Tu usuario no tiene asignado el rol/sistema 'Comité de Aula' en SASI."
                    };
                }

                return new AuthResultDto
                {
                    Exito = true,
                    Token = sasiResult.Token,
                    NombreUsuario = sasiResult.Usuario?.NombreCompleto ?? string.Empty,
                    Email = sasiResult.Usuario?.Email ?? string.Empty,
                    SistemaComite = sistemaComite
                };
            }
            catch (Exception ex)
            {
                return new AuthResultDto { Exito = false, Mensaje = $"Error al conectar con el servidor de autenticación SASI: {ex.Message}" };
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
