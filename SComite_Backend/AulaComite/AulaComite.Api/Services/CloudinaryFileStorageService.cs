using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AulaComite.Application.Common.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace AulaComite.Api.Services;

public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryFileStorageService(IConfiguration config)
    {
        var cloudName = config["Cloudinary:CloudName"]
            ?? throw new InvalidOperationException("Cloudinary:CloudName no está configurado.");
        var apiKey = config["Cloudinary:ApiKey"]
            ?? throw new InvalidOperationException("Cloudinary:ApiKey no está configurado.");
        var apiSecret = config["Cloudinary:ApiSecret"]
            ?? throw new InvalidOperationException("Cloudinary:ApiSecret no está configurado.");

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true; // Forzar siempre conexiones HTTPS seguras
    }

    public async Task<string> GuardarComprobanteAsync(byte[] contenido, string nombreOriginal, CancellationToken cancellationToken = default)
    {
        if (contenido == null || contenido.Length == 0)
            throw new ArgumentException("No se ha proporcionado un archivo válido.");

        using var stream = new MemoryStream(contenido);
        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        var publicId = $"Comprobante_{Guid.NewGuid()}";

        string urlResultado;

        // Manejo si es PDF
        if (extension == ".pdf")
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(nombreOriginal, stream),
                Folder = "comprobantes_comite",
                PublicId = publicId,
                AccessMode = "public"
            };

            // 🚀 Para archivos PDF/RAW se invoca UploadAsync enviando la sobrecarga con RawUploadParams
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            urlResultado = uploadResult.SecureUrl?.ToString() ?? string.Empty;
        }
        // Manejo si es Imagen (JPG, PNG, WEBP)
        else
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(nombreOriginal, stream),
                Folder = "comprobantes_comite",
                PublicId = publicId,
                AccessMode = "public",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            urlResultado = uploadResult.SecureUrl?.ToString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(urlResultado))
            throw new Exception("Error al subir el comprobante a la nube.");

        return urlResultado;
    }

    public void EliminarComprobante(string? urlRelativaOrAbsoluta)
    {
        if (string.IsNullOrWhiteSpace(urlRelativaOrAbsoluta)) return;

        try
        {
            var isPdf = urlRelativaOrAbsoluta.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            var publicId = ExtraerPublicId(urlRelativaOrAbsoluta, isPdf);

            if (!string.IsNullOrEmpty(publicId))
            {
                if (isPdf)
                {
                    // Para PDFs en Cloudinary, el ResourceType DEBE SER Raw y el publicId DEBE incluir la extensión .pdf
                    var deletionParams = new DeletionParams(publicId)
                    {
                        ResourceType = ResourceType.Raw,
                        Invalidate = true
                    };
                    _cloudinary.Destroy(deletionParams);
                }
                else
                {
                    // Para Imágenes (jpg, png, webp) el PublicID va SIN extensión y ResourceType es Image
                    var deletionParams = new DeletionParams(publicId)
                    {
                        ResourceType = ResourceType.Image,
                        Invalidate = true
                    };
                    _cloudinary.Destroy(deletionParams);
                }
            }
        }
        catch
        {
            // Se omite para no interrumpir la transacción principal en caso de falla de red
        }
    }

    private static string ExtraerPublicId(string url, bool mantenerExtension)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath; // ej: /aerof1gd/raw/upload/v1786021379/comprobantes_comite/Comprobante_abc.pdf

            var indexFolder = path.IndexOf("comprobantes_comite/", StringComparison.OrdinalIgnoreCase);
            if (indexFolder < 0) return string.Empty;

            var pathFromFolder = path.Substring(indexFolder); // comprobantes_comite/Comprobante_abc.pdf

            if (mantenerExtension)
            {
                // Los archivos RAW (PDF) requieren mantener el ".pdf" en su PublicID
                return pathFromFolder;
            }

            // Las imágenes NO llevan la extensión en el PublicID
            var extensionIndex = pathFromFolder.LastIndexOf('.');
            return extensionIndex > 0 ? pathFromFolder.Substring(0, extensionIndex) : pathFromFolder;
        }
        catch
        {
            return string.Empty;
        }
    }
}
