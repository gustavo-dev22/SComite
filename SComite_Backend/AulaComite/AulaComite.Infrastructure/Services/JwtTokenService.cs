using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AulaComite.Infrastructure.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _settings;

        public JwtTokenService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public string GenerarToken(SasiUsuario usuario, SasiSistema sistemaComite)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id),
                new Claim(ClaimTypes.Name, usuario.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrWhiteSpace(usuario.NombreCompleto))
            {
                claims.Add(new Claim("nombreCompleto", usuario.NombreCompleto));
            }

            if (!string.IsNullOrWhiteSpace(usuario.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, usuario.Email));
            }

            foreach (var rol in sistemaComite.Roles.Where(r => !string.IsNullOrWhiteSpace(r.NombreRol)))
            {
                claims.Add(new Claim(ClaimTypes.Role, rol.NombreRol));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(_settings.ExpirationHours),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}