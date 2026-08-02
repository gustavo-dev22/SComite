using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Comite.Dtos
{
    public class AuditoriaLecturaDto
    {
        public int EstudianteId { get; set; }
        public string NombreEstudiante { get; set; } = string.Empty;
        public string NombreApoderado { get; set; } = string.Empty;
        public string TelefonoApoderado { get; set; } = string.Empty;
        public bool Leido { get; set; }
        public DateTime? FechaLectura { get; set; }
    }

    public class ResumenAuditoriaAnuncioDto
    {
        public int AnuncioId { get; set; }
        public int TotalEstudiantesAula { get; set; }
        public int TotalLeidos { get; set; }
        public int TotalPendientes { get; set; }
        public List<AuditoriaLecturaDto> Lecturas { get; set; } = new();
    }
}
