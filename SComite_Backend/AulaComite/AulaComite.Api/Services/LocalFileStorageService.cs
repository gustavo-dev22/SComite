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

        ComprobanteFileValidator.Validar(null, nombreOriginal, contenido.CanSeek ? contenido.Length : (long?)null);

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

        var filePath = Path.Combine(ObtenerRutaCarpeta(), fileName);

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

            var filePath = Path.Combine(ObtenerRutaCarpeta(), fileName);

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
