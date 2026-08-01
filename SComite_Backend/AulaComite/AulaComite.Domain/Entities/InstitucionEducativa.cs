using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class InstitucionEducativa
    {
        public int Id { get; set; }
        public string NombreInstitucion { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? UrlLogo { get; set; }
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
        public string UsuarioActualizacion { get; set; } = string.Empty;
    }
}
