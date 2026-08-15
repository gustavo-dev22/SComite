namespace AulaComite.Application.Cuotas.Dtos
{
    public class CuotaDetalleInfoDto
    {
        public string Concepto { get; set; } = string.Empty;
        public string EstudianteNombreCompleto { get; set; } = string.Empty;
        public decimal MontoAsignado { get; set; }
        public decimal MontoPagado { get; set; }
    }
}