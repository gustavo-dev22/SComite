using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AulaComite.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace AulaComite.Api.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> GuardarComprobanteAsync(byte[] contenido, string nombreOriginal, CancellationToken cancellationToken = default)
    {
        if (contenido == null || contenido.Length == 0)
            throw new ArgumentException("No se ha proporcionado un archivo válido.");

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".webp" };
        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();

        if (!extensionesPermitidas.Contains(extension))
            throw new ArgumentException("Formato no permitido. Solo se aceptan imágenes (JPG, PNG, WEBP) o PDF.");

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var folderPath = Path.Combine(webRoot, "uploads", "comprobantes");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileName = $"Comprobante_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(folderPath, fileName);

        await File.WriteAllBytesAsync(filePath, contenido, cancellationToken);

        return $"/uploads/comprobantes/{fileName}";
    }

    public void EliminarComprobante(string? urlRelativa)
    {
        if (string.IsNullOrWhiteSpace(urlRelativa)) return;

        try
        {
            var rutaLimpia = urlRelativa.TrimStart('/', '\\');
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRoot, rutaLimpia);

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
}