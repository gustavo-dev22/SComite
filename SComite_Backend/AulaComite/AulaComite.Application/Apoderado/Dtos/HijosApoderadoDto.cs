using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Apoderado.Dtos
{
    public class HijoApoderadoDto
    {
        public int EstudianteId { get; set; }
        public string NombreEstudiante { get; set; } = string.Empty;
        public int AulaId { get; set; }
        public string NombreAula { get; set; } = string.Empty;
        public string Nivel { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
        public string TesoreroNombre { get; set; } = string.Empty;
        public string TesoreroTelefono { get; set; } = string.Empty;
    }

    public class CuotaApoderadoDto
    {
        public int CuotaId { get; set; }
        public int? CuotaDetalleId { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string TipoCuota { get; set; } = string.Empty;
        public decimal MontoTotalCuota { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal MontoPendiente { get; set; }
        public string EstadoPago { get; set; } = "PENDIENTE";
        public string EstadoVisual { get; set; } = "PENDIENTE"; // PAGADO, VENCIDO, PENDIENTE
        public string? MotivoExoneracion { get; set; }
        public DateTime? FechaPago { get; set; }
    }

    public class ResumenPagosApoderadoDto
    {
        public int EstudianteId { get; set; }
        public decimal TotalPendiente { get; set; }
        public decimal TotalPagado { get; set; }
        public int CantidadVencidas { get; set; }
        public List<CuotaApoderadoDto> Cuotas { get; set; } = new();
    }
}
