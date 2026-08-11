using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class Cuota
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
        public string TipoCuota { get; set; } = "EXTRAORDINARIA"; // EXTRAORDINARIA, RECURRENTE_MENSUAL
        public int? MesCorrespondiente { get; set; }

        /// <summary>
        /// Indica si la cuota ya venció a la fecha de referencia indicada.
        /// </summary>
        public bool EstaVencida(DateTime fechaReferencia)
        {
            return FechaVencimiento < fechaReferencia;
        }

        public bool EstaEnCobro() => string.Equals(Estado, "EN COBRO", StringComparison.OrdinalIgnoreCase);
    }
}
