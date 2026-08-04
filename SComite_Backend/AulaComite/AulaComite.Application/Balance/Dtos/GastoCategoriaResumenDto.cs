namespace AulaComite.Application.Balance.Dtos
{
    public class GastoCategoriaResumenDto
    {
        public string Categoria { get; set; } = string.Empty;
        public decimal TotalMonto { get; set; }
        public int CantidadRegistros { get; set; }
    }
}