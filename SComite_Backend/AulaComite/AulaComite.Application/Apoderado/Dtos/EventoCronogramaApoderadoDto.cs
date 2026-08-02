using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Dtos
{
    public class EventoCronogramaApoderadoDto
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string NombreActividad { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaProgramada { get; set; }
        public decimal MontoPresupuestado { get; set; }
        public decimal CuotaSugeridaPorAlumno { get; set; }
        public string Estado { get; set; } = "PLANIFICADA";
    }
}
