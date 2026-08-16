using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Exceptions
{
    /// <summary>
    /// 🛡️ Se lanza cuando el servicio de autenticación externo (SASI) no está disponible
    /// (caído, sin conexión o timeout). Permite a la API responder con 503 y un mensaje
    /// amigable, en lugar de tragar el error y devolver datos vacíos que confundan al usuario.
    /// </summary>
    public class SasiNoDisponibleException : Exception
    {
        public SasiNoDisponibleException(string mensaje)
            : base(mensaje)
        {
        }

        public SasiNoDisponibleException(string mensaje, Exception innerException)
            : base(mensaje, innerException)
        {
        }
    }
}
