using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Common;

namespace AulaComite.Domain.Entities
{
    public class InstitucionEducativa
    {
        public int Id { get; set; }
        public string NombreInstitucion { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? UrlLogo { get; set; }
        public DateTime FechaActualizacion { get; set; } = DateTimeHelper.ObtenerHoraPeru();
        public string UsuarioActualizacion { get; set; } = string.Empty;

        /// <summary>
        /// Actualiza los datos de la institución registrando fecha y usuario de auditoría.
        /// </summary>
        public void ActualizarDatos(string nombreInstitucion, string? direccion, string? urlLogo, string usuario)
        {
            NombreInstitucion = nombreInstitucion;
            Direccion = direccion;
            UrlLogo = urlLogo;
            UsuarioActualizacion = usuario;
            FechaActualizacion = DateTimeHelper.ObtenerHoraPeru();
        }
    }
}
