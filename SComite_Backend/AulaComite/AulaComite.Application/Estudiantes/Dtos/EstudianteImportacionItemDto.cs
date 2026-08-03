using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Estudiantes.Dtos
{
    public class EstudianteImportacionItemDto
    {
        public string TipoDocumento { get; set; } = "DNI";
        public string NumeroDocumento { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string? UsuarioIdApoderadoSasi { get; set; }
        public string? NombreApoderado { get; set; }
        public string? TelefonoApoderado { get; set; }
    }

    public class CargaMasivaResultadoDto
    {
        public int RegistrosProcesados { get; set; }
        public int RegistrosInsertados { get; set; }
        public int RegistrosOmitidos { get; set; }
        public List<string> ErroresValidacion { get; set; } = new();
        public List<string> DetallesObservaciones { get; set; } = new();
    }
}
