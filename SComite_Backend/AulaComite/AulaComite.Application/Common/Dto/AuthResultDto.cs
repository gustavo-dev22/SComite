using AulaComite.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Dto
{
    public class AuthResultDto
    {
        public bool Exito { get; set; }
        public bool Bloqueado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public SasiSistema? SistemaComite { get; set; }
    }
}
