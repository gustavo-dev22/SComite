using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class BalanceConsolidado
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

    public class GastoCategoriaResumen
    {
        public string Categoria { get; set; } = string.Empty;
        public decimal TotalMonto { get; set; }
        public int CantidadRegistros { get; set; }
    }
}
