using FluentValidation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AulaComite.Application.Common.Security
{
    /// <summary>
    /// Reglas de validación centralizadas para archivos de comprobantes financieros:
    /// tamaño máximo, extensión, tipo MIME y formato real (magic bytes).
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
        /// 🛡️ Validación completa de comprobante (tamaño, MIME, extensión) + verificación del
        /// FORMATO REAL mediante magic bytes del contenido, de modo que no baste con renombrar
        /// un archivo a .pdf/.jpg para que sea aceptado. Requiere un <see cref="Stream"/> con
        /// capacidad de rewind (<see cref="Stream.CanSeek"/>); si no lo permite, se conserva la
        /// validación por extensión/MIME como red de seguridad. Se restaura la posición original.
        /// </summary>
        public static void Validar(string? contentType, string? nombreOriginal, long? longitud, Stream contenido)
        {
            Validar(contentType, nombreOriginal, longitud);

            if (contenido == null)
                throw new ValidationException("No se ha proporcionado un archivo válido.");

            // Solo se puede inspeccionar el contenido si el stream permite retroceder (rewind).
            if (!contenido.CanSeek)
                return;

            var posicionOriginal = contenido.Position;

            try
            {
                contenido.Position = 0;

                // Se lee hasta 12 bytes: suficiente para todas las firmas (PDF=4, JPEG=3, PNG=8, WEBP=12).
                var cabecera = new byte[12];
                var totalLeidos = 0;
                while (totalLeidos < cabecera.Length)
                {
                    var leidos = contenido.Read(cabecera, totalLeidos, cabecera.Length - totalLeidos);
                    if (leidos <= 0) break;
                    totalLeidos += leidos;
                }

                var formatoReal = DetectarFormatoReal(cabecera, totalLeidos);
// nombreOriginal ya fue validado como no nulo por la validación base (lanza si es nulo).
                var extension = Path.GetExtension(nombreOriginal!).ToLowerInvariant();

                if (formatoReal == null || !FormatoCoincideConExtension(formatoReal, extension))
                    throw new ValidationException("El contenido del archivo no coincide con el formato declarado. Solo se aceptan imágenes (JPG, PNG, WEBP) o PDF.");
            }
            finally
            {
                contenido.Position = posicionOriginal;
            }
        }

        /// <summary>
        /// Detecta el formato real del archivo a partir de sus magic bytes iniciales.
        /// </summary>
        private static string? DetectarFormatoReal(byte[] cabecera, int longitud)
        {
            if (longitud >= 4
                && cabecera[0] == 0x25 && cabecera[1] == 0x50 && cabecera[2] == 0x44 && cabecera[3] == 0x46)
                return "pdf";

            if (longitud >= 3
                && cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF)
                return "jpg";

            if (longitud >= 8
                && cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47
                && cabecera[4] == 0x0D && cabecera[5] == 0x0A && cabecera[6] == 0x1A && cabecera[7] == 0x0A)
                return "png";

            if (longitud >= 12
                && cabecera[0] == 0x52 && cabecera[1] == 0x49 && cabecera[2] == 0x46 && cabecera[3] == 0x46
                && cabecera[8] == 0x57 && cabecera[9] == 0x45 && cabecera[10] == 0x42 && cabecera[11] == 0x50)
                return "webp";

            return null;
        }

        /// <summary>
        /// Indica si el formato detectado por magic bytes corresponde a la extensión declarada.
        /// </summary>
        private static bool FormatoCoincideConExtension(string formato, string extension)
        {
            return extension switch
            {
                ".pdf" => formato == "pdf",
                ".jpg" or ".jpeg" => formato == "jpg",
                ".png" => formato == "png",
                ".webp" => formato == "webp",
                _ => false
            };
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
