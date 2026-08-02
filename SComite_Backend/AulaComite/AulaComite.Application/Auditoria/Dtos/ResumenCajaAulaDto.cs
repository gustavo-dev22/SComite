using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Auditoria.Dtos
{
    public class ResumenCajaAulaDto
    {
        public int AulaId { get; set; }
        public string Nivel { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
        public string NombreAula { get; set; } = string.Empty;
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal SaldoNeto { get; set; }
        public string EstadoFinanciero { get; set; } = "AL_DIA"; // AL_DIA, SIN_MOVIMIENTO, ALERTA_ROJO
    }

    public class ResumenGeneralCajasConsolidadasDto
    {
        public decimal TotalIngresosInstitucional { get; set; }
        public decimal TotalEgresosInstitucional { get; set; }
        public decimal SaldoNetoInstitucional { get; set; }
        public int TotalAulas { get; set; }
        public int AulasAlDia { get; set; }
        public int AulasSinMovimiento { get; set; }
        public int AulasEnAlerta { get; set; }
        public List<ResumenCajaAulaDto> DetalleAulas { get; set; } = new();
    }
}
