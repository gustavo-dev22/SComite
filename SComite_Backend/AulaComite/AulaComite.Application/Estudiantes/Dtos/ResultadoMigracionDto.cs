using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Estudiantes.Dtos
{
    public class ResultadoMigracionDto
    {
        public int Solicitados { get; set; }
        public int Migrados { get; set; }
        public int Omitidos { get; set; }
        public List<DetalleOmitidoDto> Detalles { get; set; } = new();
    }

    public class DetalleOmitidoDto
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
    }
}
