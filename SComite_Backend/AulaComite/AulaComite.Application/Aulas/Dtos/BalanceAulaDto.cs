using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Aulas.Dtos
{
    public class BalanceAulaDto
    {
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal SaldoDisponible { get; set; }
        public IEnumerable<BalanceMensualDto> DesgloseMensual { get; set; } = new List<BalanceMensualDto>();
        public IEnumerable<GastoTransparenciaDto> Egresos { get; set; } = new List<GastoTransparenciaDto>();
    }

    public class BalanceMensualDto
    {
        public int Anio { get; set; }
        public int MesNum { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public decimal TotalIngresosMes { get; set; }
        public decimal TotalEgresosMes { get; set; }
        public decimal SaldoMes { get; set; }
    }

    public class GastoTransparenciaDto
    {
        public int Id { get; set; }
        public DateTime FechaGasto { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string? Proveedor { get; set; }
        public string? TipoComprobante { get; set; }
        public string? NumeroComprobante { get; set; }
        public string? UrlComprobante { get; set; }
    }
}
