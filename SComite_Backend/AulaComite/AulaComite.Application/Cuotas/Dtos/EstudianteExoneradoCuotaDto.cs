using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Cuotas.Dtos
{
    public class EstudianteExoneradoCuotaDto
    {
        public int CuotaDetalleId { get; set; }
        public int EstudianteId { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreEstudiante { get; set; } = string.Empty;
        public string NombreApoderado { get; set; } = string.Empty;
        public string TelefonoApoderado { get; set; } = string.Empty;
        public decimal MontoAsignado { get; set; }
        public string MotivoExoneracion { get; set; } = string.Empty;
        public DateTime? FechaExoneracion { get; set; }
    }
}
