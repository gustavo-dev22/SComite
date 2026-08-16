namespace AulaComite.Application.Common.Interfaces
{
    public interface ISasiTokenStore
    {
        void Guardar(string usuarioId, string token);
        string? Obtener(string usuarioId);
        void Limpiar(string usuarioId);
    }
}
