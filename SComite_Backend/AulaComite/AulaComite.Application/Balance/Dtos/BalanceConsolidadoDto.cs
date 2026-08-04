namespace AulaComite.Application.Balance.Dtos
{
    public class BalanceConsolidadoDto
    {
        public decimal SaldoAnteriorArrastrado { get; set; }
        public decimal IngresosMensuales { get; set; }
        public decimal IngresosExtraordinarios { get; set; }
        public decimal IngresosDonaciones { get; set; }
        public decimal TotalIngresosMes { get; set; }
        public decimal TotalEgresosMes { get; set; }
        public decimal SaldoNetoEnCaja { get; set; }
        public decimal TotalPorCobrar { get; set; }
        public decimal PorcentajeCumplimiento { get; set; }
    }
}