using System.Collections.Concurrent;
using AulaComite.Application.Common.Interfaces;

namespace AulaComite.Infrastructure.Services
{
    public class SasiTokenStore : ISasiTokenStore
    {
        private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.OrdinalIgnoreCase);

        public void Guardar(string usuarioId, string token)
        {
            _tokens[usuarioId] = token;
        }

        public string? Obtener(string usuarioId)
        {
            return _tokens.TryGetValue(usuarioId, out var token) ? token : null;
        }

        public void Limpiar(string usuarioId)
        {
            _tokens.TryRemove(usuarioId, out _);
        }
    }
}
