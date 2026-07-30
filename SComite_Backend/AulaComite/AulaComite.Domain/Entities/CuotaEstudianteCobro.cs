using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class CuotaEstudianteCobro
    {
        public int CuotaDetalleId { get; set; }
        public int CuotaId { get; set; }
        public int EstudianteId { get; set; }
        public string EstudianteNombreCompleto { get; set; } = string.Empty;
        public string EstudianteDocumento { get; set; } = string.Empty;
        public string NombreApoderado { get; set; } = string.Empty;
        public string TelefonoApoderado { get; set; } = string.Empty;
        public decimal MontoAsignado { get; set; }
        public decimal MontoPagado { get; set; }
        public string EstadoPago { get; set; } = "PENDIENTE"; // PENDIENTE, PARCIAL, COMPLETO
        public DateTime? FechaUltimoPago { get; set; }
    }
}
