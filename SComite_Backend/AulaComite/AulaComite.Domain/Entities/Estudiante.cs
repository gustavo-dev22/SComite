using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Domain.Entities
{
    public class Estudiante
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

        /// <summary>
        /// Construye el nombre completo en el formato estándar del padrón:
        /// "ApellidoPaterno ApellidoMaterno, Nombres".
        /// </summary>
        public string CalcularNombreCompleto()
        {
            var apellidos = $"{ApellidoPaterno} {ApellidoMaterno}".Trim();
            return string.IsNullOrWhiteSpace(apellidos)
                ? Nombres.Trim()
                : $"{apellidos}, {Nombres.Trim()}".Trim();
        }

        /// <summary>
        /// Actualiza los datos editables de la ficha del estudiante.
        /// </summary>
        public void ActualizarDatos(
            int aulaId,
            string tipoDocumento,
            string numeroDocumento,
            string nombres,
            string apellidoPaterno,
            string apellidoMaterno,
            string? usuarioIdApoderadoSasi,
            string? nombreApoderado,
            string? telefonoApoderado)
        {
            AulaId = aulaId;
            TipoDocumento = tipoDocumento;
            NumeroDocumento = numeroDocumento;
            Nombres = nombres;
            ApellidoPaterno = apellidoPaterno;
            ApellidoMaterno = apellidoMaterno;
            UsuarioIdApoderadoSasi = usuarioIdApoderadoSasi;
            NombreApoderado = nombreApoderado;
            TelefonoApoderado = telefonoApoderado;
            NombreCompleto = CalcularNombreCompleto();
        }

        public void Desactivar() => Estado = false;

        public void Activar() => Estado = true;

        public bool EstaActivo() => Estado;
    }
}
