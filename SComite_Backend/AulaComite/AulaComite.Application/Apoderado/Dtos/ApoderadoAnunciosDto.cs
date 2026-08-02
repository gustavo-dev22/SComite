using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Dtos
{
    public class AnuncioApoderadoDto
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty; // INFORMATIVO, URGENTE, CITACION, TESORERIA, EVENTO
        public bool EsFijado { get; set; }
        public string? UrlAdjunto { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaPublicacion { get; set; }
        public int CantidadVistas { get; set; }
        public bool Leido { get; set; }
    }
}
