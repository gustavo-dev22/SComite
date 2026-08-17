using System.Collections.Concurrent;
using AulaComite.Application.Common.Interfaces;

namespace AulaComite.Infrastructure.Services
{
    public class SasiTokenStore : ISasiTokenStore
    {
        private readonly ConcurrentDictionary<string, SasiTokenInfo> _tokens = new(StringComparer.OrdinalIgnoreCase);

        public void Guardar(string usuarioId, string token, string refreshToken)
        {
            _tokens[usuarioId] = new SasiTokenInfo { Token = token, RefreshToken = refreshToken };
        }

        public SasiTokenInfo? Obtener(string usuarioId)
        {
            return _tokens.TryGetValue(usuarioId, out var token) ? token : null;
        }

        public void Limpiar(string usuarioId)
        {
            _tokens.TryRemove(usuarioId, out _);
        }
    }
}
