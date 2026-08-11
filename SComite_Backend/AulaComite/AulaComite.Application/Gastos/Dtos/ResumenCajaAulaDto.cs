namespace AulaComite.Application.Gastos.Dtos
{
    /// <summary>
    /// Resumen de caja de un Aula. Proyección de consulta trasladada desde el Dominio.
    /// </summary>
    public class ResumenCajaAulaDto
    {
        public decimal SaldoAnteriorArrastrado { get; set; }
        public decimal IngresosDelMes { get; set; }
        public decimal MontoDonacionesMes { get; set; }
        public decimal EgresosDelMes { get; set; }
        public decimal SaldoDisponibleReal { get; set; }
    }
}