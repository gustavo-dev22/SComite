namespace AulaComite.Application.Cuotas.Dtos
{
    public class CuotaDto
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public int? ActividadId { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public decimal MontoIndividual { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string Estado { get; set; } = "EN COBRO";
        public string? Observacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string TipoCuota { get; set; } = "EXTRAORDINARIA";
        public int? MesCorrespondiente { get; set; }
        public int TotalEstudiantesAsignados { get; set; }
        public decimal TotalMontoEsperado { get; set; }
        public decimal TotalMontoRecaudado { get; set; }
        public int EstudiantesAlDia { get; set; }
        public int EstudiantesPendientes { get; set; }
    }
}