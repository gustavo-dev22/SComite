namespace AulaComite.Application.Common.Interfaces
{
    public interface ISasiTokenStore
    {
        void Guardar(string usuarioId, string token, string refreshToken);
        SasiTokenInfo? Obtener(string usuarioId);
        void Limpiar(string usuarioId);
    }

    public class SasiTokenInfo
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
