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
        public string? CodigoModular { get; set; }
        public string? LemaInstitucional { get; set; }
        public string? NombreDirector { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoContacto { get; set; }
        public string? UrlLogo { get; set; }
        public DateTime FechaActualizacion { get; set; } = DateTimeHelper.ObtenerHoraPeru();
        public string UsuarioActualizacion { get; set; } = string.Empty;

        /// <summary>
        /// Actualiza los datos de la institución registrando fecha y usuario de auditoría.
        /// </summary>
        public void ActualizarDatos(string nombreInstitucion, string? codigoModular, string? lemaInstitucional,
            string? nombreDirector, string? direccion, string? telefono, string? correoContacto, string? urlLogo, string usuario)
        {
            NombreInstitucion = nombreInstitucion;
            CodigoModular = codigoModular;
            LemaInstitucional = lemaInstitucional;
            NombreDirector = nombreDirector;
            Direccion = direccion;
            Telefono = telefono;
            CorreoContacto = correoContacto;
            UrlLogo = urlLogo;
            UsuarioActualizacion = usuario;
            FechaActualizacion = DateTimeHelper.ObtenerHoraPeru();
        }
    }
}
