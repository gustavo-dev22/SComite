using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Dtos
{
    public class ActaApoderadoDto
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string NumeroActa { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaReunion { get; set; }
        public string AgendaAcuerdos { get; set; } = string.Empty;
        public string EstadoActa { get; set; } = string.Empty;
        public string? UrlDocumentoPdf { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }
}
