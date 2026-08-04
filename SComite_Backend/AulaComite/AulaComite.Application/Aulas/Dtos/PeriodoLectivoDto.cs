namespace AulaComite.Application.Aulas.Dtos
{
    public class PeriodoLectivoDto
    {
        public int Id { get; set; }
        public int Anio { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool EsActivo { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }
}