using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class GastoComite
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string Categoria { get; set; } = "MATERIALES"; // MATERIALES, MANTENIMIENTO, ACTIVIDADES_EVENTOS, REFRIGERIOS, OTROS
        public decimal Monto { get; set; }
        public DateTime FechaGasto { get; set; }
        public string TipoComprobante { get; set; } = "BOLETA"; // BOLETA, FACTURA, RECIBO_SIMPLE, SIN_COMPROBANTE
        public string? NumeroComprobante { get; set; }
        public string? Proveedor { get; set; }
        public string? Observacion { get; set; }
        public string? UrlComprobante { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }

        public bool PerteneceAAula(int aulaId) => AulaId == aulaId;
    }
}
