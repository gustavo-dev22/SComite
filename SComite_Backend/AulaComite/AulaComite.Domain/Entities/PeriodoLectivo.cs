using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class PeriodoLectivo
    {
        public int Id { get; set; }
        public int Anio { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool EsActivo { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }
}
