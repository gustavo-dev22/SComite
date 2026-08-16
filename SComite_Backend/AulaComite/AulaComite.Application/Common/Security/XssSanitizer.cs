using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AulaComite.Application.Common.Security
{
    /// <summary>
    /// 🛡️ Sanitizador anti-XSS para campos de texto abierto (títulos, contenidos y acuerdos).
    /// Trata el valor como TEXTO PLANO escapando los caracteres que podrían interpretarse como
    /// markup/scripting al renderizarse (usa <see cref="WebUtility.HtmlEncode"/>), de modo que
    /// `<script>alert(1)</script>` se persista y devuelva de forma inofensiva.
    /// </summary>
    public static class XssSanitizer
    {
        /// <summary>
        /// Convierte el texto a texto plano seguro contra inyección HTML/Scripting.
        /// </summary>
        public static string SanitizarTextoPlano(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return valor ?? string.Empty;
            return WebUtility.HtmlEncode(valor);
        }
    }
}