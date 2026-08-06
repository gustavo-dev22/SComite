namespace AulaComite.Application.Gastos.Dtos
{
    public class GastoComiteDto
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string Categoria { get; set; } = "MATERIALES";
        public decimal Monto { get; set; }
        public DateTime FechaGasto { get; set; }
        public string TipoComprobante { get; set; } = "BOLETA";
        public string? NumeroComprobante { get; set; }
        public string? UrlComprobante { get; set; }
        public string? Proveedor { get; set; }
        public string? Observacion { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }
}