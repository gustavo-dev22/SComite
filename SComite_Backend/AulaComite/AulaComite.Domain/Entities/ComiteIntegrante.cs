using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class ComiteIntegrante
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string UsuarioIdSasi { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public DateTime FechaAsignacion { get; set; }

        public void Desactivar() => Estado = false;

        public bool EstaActivo() => Estado;
    }
}
