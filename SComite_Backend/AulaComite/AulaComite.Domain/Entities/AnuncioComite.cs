using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class AnuncioComite
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public string Categoria { get; set; } = "INFORMATIVO"; // URGENTE, CITACION, TESORERIA, EVENTO, INFORMATIVO
        public bool EsFijado { get; set; } = false;
        public string? UrlAdjunto { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaPublicacion { get; set; } = DateTime.Now;
        public int CantidadVisitas { get; set; }
        public bool Estado { get; set; }
    }
}
