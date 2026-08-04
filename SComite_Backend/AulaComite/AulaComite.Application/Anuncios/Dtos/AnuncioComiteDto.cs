namespace AulaComite.Application.Anuncios.Dtos
{
    public class AnuncioComiteDto
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public string Categoria { get; set; } = "INFORMATIVO";
        public bool EsFijado { get; set; }
        public string? UrlAdjunto { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaPublicacion { get; set; }
        public int CantidadVistas { get; set; }
        public bool Estado { get; set; }
    }
}