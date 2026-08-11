using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Security
{
    /// <summary>
    /// 🛡️ M7: Enmascara datos personales (documento, teléfono, correo) en respuestas de
    /// listados generales, de modo que solo se expongan por completo en consultas de
    /// detalle por usuarios autorizados.
    /// </summary>
    public static class PiiMasker
    {
        /// <summary>
        /// DNI/documento: conserva los 2 primeros y los 2 últimos dígitos. Ej: "12****45".
        /// </summary>
        public static string EnmascararDocumento(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
            var v = valor.Trim();
            if (v.Length <= 4) return new string('*', v.Length);
            return v.Substring(0, 2) + new string('*', v.Length - 4) + v.Substring(v.Length - 2);
        }

        /// <summary>
        /// Teléfono: conserva los 3 primeros y los 2 últimos dígitos. Ej: "987****21".
        /// </summary>
        public static string EnmascararTelefono(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
            var v = valor.Trim();

            var visiblesInicio = Math.Min(3, v.Length);
            var visiblesFin = Math.Min(2, Math.Max(0, v.Length - visiblesInicio));
            var ocultos = v.Length - visiblesInicio - visiblesFin;

            return v.Substring(0, visiblesInicio) + new string('*', ocultos) + (visiblesFin > 0 ? v.Substring(v.Length - visiblesFin) : string.Empty);
        }

        /// <summary>
        /// Correo: conserva el dominio y los 2 primeros caracteres de la parte local.
        /// Ej: "na***@dominio.com".
        /// </summary>
        public static string EnmascararEmail(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
            var v = valor.Trim();

            var arroba = v.IndexOf('@');
            if (arroba <= 1) return v;

            var parteLocal = v.Substring(0, arroba);
            var dominio = v.Substring(arroba);
            var visibles = Math.Min(2, parteLocal.Length);

            return parteLocal.Substring(0, visibles) + new string('*', Math.Max(1, parteLocal.Length - visibles)) + dominio;
        }
    }
}