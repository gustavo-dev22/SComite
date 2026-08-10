using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AulaComite.Application.Common.Security
{
    /// <summary>
    /// Reglas de validación centralizadas para archivos de comprobantes financieros:
    /// tamaño máximo, extensión y tipo MIME permitidos.
    /// </summary>
    public static class ComprobanteFileValidator
    {
        public const long MaxTamanoBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly HashSet<string> TiposMimePermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp",
            "application/pdf"
        };

        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".pdf"
        };

        public static void Validar(string? contentType, string? nombreOriginal, long? longitud)
        {
            if (string.IsNullOrWhiteSpace(nombreOriginal))
                throw new ArgumentException("No se ha seleccionado ningún archivo.");

            if (longitud.HasValue)
            {
                if (longitud.Value <= 0)
                    throw new ArgumentException("El archivo está vacío.");

                if (longitud.Value > MaxTamanoBytes)
                    throw new ArgumentException("El archivo supera el tamaño máximo permitido de 5 MB.");
            }

            var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
                throw new ArgumentException("Formato no permitido. Solo se aceptan imágenes (JPG, PNG, WEBP) o PDF.");

            if (!string.IsNullOrWhiteSpace(contentType) && !TiposMimePermitidos.Contains(contentType))
                throw new ArgumentException("Tipo de archivo no permitido. Solo se aceptan imágenes (JPG, PNG, WEBP) o PDF.");
        }
    }
}
