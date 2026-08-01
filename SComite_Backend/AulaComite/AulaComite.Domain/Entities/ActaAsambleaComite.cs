using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class ActaAsambleaComite
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string NumeroActa { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaReunion { get; set; }
        public string AgendaAcuerdos { get; set; } = string.Empty;
        public string EstadoActa { get; set; } = "BORRADOR";
        public string? UrlDocumentoPdf { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public string? UsuarioActualizacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public bool Estado { get; set; } = true;
    }
}
