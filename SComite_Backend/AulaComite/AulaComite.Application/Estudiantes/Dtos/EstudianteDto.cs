namespace AulaComite.Application.Estudiantes.Dtos
{
    public class EstudianteDto
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string TipoDocumento { get; set; } = "DNI";
        public string NumeroDocumento { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string? NombreCompleto { get; set; }
        public string? UsuarioIdApoderadoSasi { get; set; }
        public string? NombreApoderado { get; set; }
        public string? TelefonoApoderado { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}