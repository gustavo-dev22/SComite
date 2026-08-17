using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Models
{
    public class LoginRequestDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SasiLoginResponse
    {
        public bool Success { get; set; }
        public bool Bloqueado { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public SasiUsuario? Usuario { get; set; }
    }

    public class SasiRefreshResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class SasiUsuario
    {
        public string Id { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<SasiSistema> Sistemas { get; set; } = new();
    }

    public class SasiSistema
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public List<SasiRol> Roles { get; set; } = new();
    }

    public class SasiRol
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public bool EsPrincipal { get; set; }
        public List<SasiObjetoMenu> Objetos { get; set; } = new();
    }

    public class SasiObjetoMenu
    {
        public int IdObjeto { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // "Menu" o "Submenu"
        public string? Url { get; set; }
        public string? Titulo { get; set; }
        public string? Icono { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public int? IdPadre { get; set; }
    }
}
