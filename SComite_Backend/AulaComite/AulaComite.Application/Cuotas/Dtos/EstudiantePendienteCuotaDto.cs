using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Cuotas.Dtos
{
    public class EstudiantePendienteCuotaDto
    {
        public int EstudianteId { get; set; }
        public string TipoDocumento { get; set; } = "DNI";
        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreEstudiante { get; set; } = string.Empty;
        public string NombreApoderado { get; set; } = string.Empty;
        public string TelefonoApoderado { get; set; } = string.Empty;
        public decimal MontoAsignado { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal MontoPendiente { get; set; }
        public string EstadoPago { get; set; } = "PENDIENTE";
    }
}
