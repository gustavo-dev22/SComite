using AulaComite.Application.Common.Models;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerarToken(SasiUsuario usuario, SasiSistema sistemaComite);
    }
}