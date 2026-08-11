using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Common;

namespace AulaComite.Domain.Entities
{
    public class ActividadComite
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string NombreActividad { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaProgramada { get; set; }
        public decimal MontoPresupuestado { get; set; }
        public decimal CuotaSugeridaPorAlumno { get; set; }
        public string Estado { get; set; } = "PLANIFICADA"; // PLANIFICADA, EN_PROCESO, FINALIZADA, CANCELADA
        public DateTime FechaRegistro { get; set; } = DateTimeHelper.ObtenerHoraPeru();

        private static readonly string[] EstadosValidos =
        {
            "PLANIFICADA", "EN_PROCESO", "FINALIZADA", "CANCELADA"
        };

        /// <summary>
        /// Cambia el estado de la actividad validando que sea uno permitido.
        /// </summary>
        public void CambiarEstado(string nuevoEstado)
        {
            if (!EstadosValidos.Contains(nuevoEstado))
                throw new ArgumentException($"El estado '{nuevoEstado}' no es válido para una actividad.");

            Estado = nuevoEstado;
        }

        public bool EstaCancelada() => string.Equals(Estado, "CANCELADA", StringComparison.OrdinalIgnoreCase);
    }
}
