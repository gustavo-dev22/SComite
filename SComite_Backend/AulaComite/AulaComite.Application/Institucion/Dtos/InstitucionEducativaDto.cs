namespace AulaComite.Application.Institucion.Dtos
{
    public class InstitucionEducativaDto
    {
        public int Id { get; set; }
        public string NombreInstitucion { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? UrlLogo { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public string UsuarioActualizacion { get; set; } = string.Empty;
    }
}