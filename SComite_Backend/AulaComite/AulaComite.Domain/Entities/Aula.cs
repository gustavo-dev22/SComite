using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class Aula
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public string Nivel { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
        public string? NombreDisplay { get; set; }
        public bool Estado { get; set; }
        public string AnioPeriodo { get; set; } = string.Empty;
    }
}
