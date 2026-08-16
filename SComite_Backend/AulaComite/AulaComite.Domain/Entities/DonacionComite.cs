using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class DonacionComite
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string Donante { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTime FechaDonacion { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string? Observacion { get; set; }
        public DateTime FechaRegistro { get; set; }

        public bool PerteneceAAula(int aulaId) => AulaId == aulaId;
    }
}
