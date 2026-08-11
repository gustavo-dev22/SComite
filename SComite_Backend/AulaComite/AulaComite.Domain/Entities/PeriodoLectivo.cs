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

        /// <summary>
        /// Indica si la fecha dada se encuentra dentro del rango del periodo lectivo.
        /// </summary>
        public bool EstaVigente(DateTime fecha)
        {
            return fecha >= FechaInicio && fecha <= FechaFin;
        }

        public bool EstaActivoEn(DateTime fecha) => EsActivo && EstaVigente(fecha);
    }
}
