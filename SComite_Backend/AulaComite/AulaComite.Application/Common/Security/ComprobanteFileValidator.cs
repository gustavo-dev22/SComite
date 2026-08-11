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

            // 🛡️ M5: rechaza nombres con navegación de directorios ("../") o separadores de ruta.
            ObtenerNombreArchivoSeguro(nombreOriginal);

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

        /// <summary>
        /// 🛡️ M5: Sanea un nombre de archivo para forzar que sea SOLO el nombre base
        /// (sin directorios, rutas raíz ni secuencias de navegación) y que no contenga
        /// caracteres no válidos. Lanza <see cref="ArgumentException"/> si no es seguro.
        /// </summary>
        public static string ObtenerNombreArchivoSeguro(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("Nombre de archivo no válido.");

            var nombre = valor.Trim();

            // Rechazar secuencias de navegación de directorios ("..", "../", "..\")
            if (nombre.Contains("..", StringComparison.Ordinal))
                throw new ArgumentException("Nombre de archivo no válido.");

            // Rechazar separadores de ruta (evita subcarpetas y rutas absolutas/UNC)
            if (nombre.IndexOfAny(new[] { '/', '\\' }) >= 0)
                throw new ArgumentException("Nombre de archivo no válido.");

            // Forzar que el valor sea únicamente el nombre de un archivo (sin carpeta ni drive).
            var nombreBase = Path.GetFileName(nombre);
            if (string.IsNullOrEmpty(nombreBase) || !string.Equals(nombreBase, nombre, StringComparison.Ordinal))
                throw new ArgumentException("Nombre de archivo no válido.");

            // Rechazar caracteres no permitidos en nombres de archivo del sistema operativo.
            if (nombre.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Nombre de archivo no válido.");

            return nombreBase;
        }
    }
}
