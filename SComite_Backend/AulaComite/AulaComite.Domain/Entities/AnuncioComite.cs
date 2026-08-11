using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Common;

namespace AulaComite.Domain.Entities
{
    public class AnuncioComite
    {
        public int Id { get; set; }
        public int AulaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public string Categoria { get; set; } = "INFORMATIVO"; // URGENTE, CITACION, TESORERIA, EVENTO, INFORMATIVO
        public bool EsFijado { get; set; } = false;
        public string? UrlAdjunto { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaPublicacion { get; set; } = DateTimeHelper.ObtenerHoraPeru();
        public int CantidadVistas { get; set; }
        public bool Estado { get; set; }

        /// <summary>
        /// Registra una visualización adicional del anuncio en el muro del aula.
        /// </summary>
        public void RegistrarVista()
        {
            CantidadVistas++;
        }

        public void FijarEnMuro(bool fijado) => EsFijado = fijado;

        public bool EstaPublicado() => Estado;
    }
}
