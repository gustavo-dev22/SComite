using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Dto
{
    public class UsuarioSasiDto
    {
        public string UsuarioId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class SasiResponseDto<T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public T? Datos { get; set; }
    }
}
