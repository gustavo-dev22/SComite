using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using Microsoft.AspNetCore.Hosting;

namespace AulaComite.Api.Services;

public class LocalFileStorageService : IFileStorageService
{
    // Carpeta privada FUERA de wwwroot para que NUNCA sea servida por UseStaticFiles.
    private const string CarpetaComprobantes = "private_uploads/comprobantes";

    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> GuardarComprobanteAsync(Stream contenido, string nombreOriginal, CancellationToken cancellationToken = default)
    {
        if (contenido == null)
            throw new ArgumentException("No se ha proporcionado un archivo válido.");

        // 🛡️ Incluye la verificación de magic bytes cuando el stream permite rewind.
        ComprobanteFileValidator.Validar(null, nombreOriginal, contenido.CanSeek ? contenido.Length : (long?)null, contenido);

        var folderPath = ObtenerRutaCarpeta();

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        var fileName = $"Comprobante_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(folderPath, fileName);

        // 🚀 Streaming directo: nunca se carga el buffer completo del archivo en memoria.
        await using (var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
        {
            await contenido.CopyToAsync(fileStream, cancellationToken);
        }

        // Identificador servido exclusivamente por el endpoint protegido [Authorize].
        return $"/api/gastos/comprobante?archivo={Uri.EscapeDataString(fileName)}";
    }

    public async Task<ComprobanteArchivoDescriptor?> ObtenerComprobanteAsync(string urlOIdentificador, CancellationToken cancellationToken = default)
    {
        var fileName = ExtraerNombreArchivo(urlOIdentificador);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        // 🛡️ M5: sanitizar el nombre (rechaza "../", "..\", separadores y caracteres no válidos).
        try
        {
            fileName = ComprobanteFileValidator.ObtenerNombreArchivoSeguro(fileName);
        }
        catch (ArgumentException)
        {
            return null;
        }

        var filePath = ObtenerRutaArchivoSegura(fileName);
        if (filePath == null)
            return null;

        if (!File.Exists(filePath))
            return null;

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        return new ComprobanteArchivoDescriptor(stream, ObtenerTipoContenido(fileName));
    }

    public void EliminarComprobante(string? urlOIdentificador)
    {
        if (string.IsNullOrWhiteSpace(urlOIdentificador)) return;

        try
        {
            var fileName = ExtraerNombreArchivo(urlOIdentificador);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            // 🛡️ M5: sanitizar el nombre antes de acceder al disco.
            fileName = ComprobanteFileValidator.ObtenerNombreArchivoSeguro(fileName);

            var filePath = ObtenerRutaArchivoSegura(fileName);
            if (filePath == null) return;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Se ignora para no romper transacciones en caso de error de lectura de disco
        }
    }

    private string ObtenerRutaCarpeta()
    {
        return Path.Combine(_environment.ContentRootPath, CarpetaComprobantes.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>
    /// 🛡️ M5: Devuelve la ruta completa, garantizando que el archivo esté SIEMPRE
    /// contenido dentro de la carpeta base permitida (anti Path Traversal).
    /// </summary>
    private string? ObtenerRutaArchivoSegura(string fileName)
    {
        var carpetaBase = Path.GetFullPath(ObtenerRutaCarpeta()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var rutaCompleta = Path.GetFullPath(Path.Combine(carpetaBase, fileName));

        if (!rutaCompleta.StartsWith(carpetaBase, StringComparison.OrdinalIgnoreCase))
            return null;

        return rutaCompleta;
    }

    private static string? ExtraerNombreArchivo(string urlOIdentificador)
    {
        if (string.IsNullOrWhiteSpace(urlOIdentificador)) return null;

        var valor = urlOIdentificador.Trim().Replace('\\', '/');

        // 1. Preferir el parámetro "archivo=" cuando la URL proviene del endpoint de API:
        //    "/api/gastos/comprobante?archivo=Comprobante_x.jpg"
        var indexQuery = valor.IndexOf("archivo=", StringComparison.OrdinalIgnoreCase);
        if (indexQuery >= 0)
        {
            valor = valor.Substring(indexQuery + "archivo=".Length);
            var indexAmp = valor.IndexOf('&');
            if (indexAmp >= 0) valor = valor.Substring(0, indexAmp);
            return string.IsNullOrWhiteSpace(valor) ? null : Uri.UnescapeDataString(valor);
        }

        // 2. Descartar cualquier otro query string.
        indexQuery = valor.IndexOf('?');
        if (indexQuery >= 0) valor = valor.Substring(0, indexQuery);

        // 3. Tomar el último segmento de la ruta (también admite rutas con slash).
        var index = valor.LastIndexOf('/');
        if (index >= 0) valor = valor.Substring(index + 1);

        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    private static string ObtenerTipoContenido(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}
