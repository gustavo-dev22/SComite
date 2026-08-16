using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Application.Common.Security;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace AulaComite.Api.Services;

public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly IHttpClientFactory _httpClientFactory;

    public CloudinaryFileStorageService(IConfiguration config, IHttpClientFactory httpClientFactory)
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
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GuardarComprobanteAsync(Stream contenido, string nombreOriginal, CancellationToken cancellationToken = default)
    {
        if (contenido == null)
            throw new ArgumentException("No se ha proporcionado un archivo válido.");

        // 🛡️ Incluye la verificación de magic bytes incluso si el stream no permite rewind
        // (internamente copia a un MemoryStream seguro y devuelve el stream a almacenar).
        var streamAAlmacenar = ComprobanteFileValidator.Validar(null, nombreOriginal, contenido.CanSeek ? contenido.Length : (long?)null, contenido);

        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        var publicId = $"Comprobante_{Guid.NewGuid()}";

        string urlResultado;

        // Manejo si es PDF
        if (extension == ".pdf")
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(nombreOriginal, streamAAlmacenar),
                Folder = "comprobantes_comite",
                PublicId = publicId,
                // 🛡️ Acceso AUTHENTICATED: el comprobante NO queda público irrestrictamente.
                // Solo se sirve mediante URLs firmadas a través del endpoint [Authorize].
                AccessMode = "authenticated"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            urlResultado = uploadResult.SecureUrl?.ToString() ?? string.Empty;
        }
        // Manejo si es Imagen (JPG, PNG, WEBP)
        else
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(nombreOriginal, streamAAlmacenar),
                Folder = "comprobantes_comite",
                PublicId = publicId,
                AccessMode = "authenticated",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            urlResultado = uploadResult.SecureUrl?.ToString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(urlResultado))
            throw new Exception("Error al subir el comprobante a la nube.");

        return urlResultado;
    }

    public async Task<ComprobanteArchivoDescriptor?> ObtenerComprobanteAsync(string urlOIdentificador, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(urlOIdentificador)) return null;

        var (publicId, esPdf) = ExtraerPublicId(urlOIdentificador);
        if (string.IsNullOrEmpty(publicId)) return null;

        // 🛡️ Se genera una URL FIRMADA (autenticada) para poder descargar el recurso,
        // ya que los comprobantes ya no se exponen de forma pública.
        string urlFirmada;
        if (esPdf)
        {
            urlFirmada = _cloudinary.Api.Url
                .Secure()
                .Signed(true)
                .ResourceType("raw")
                .BuildUrl(publicId);
        }
        else
        {
            urlFirmada = _cloudinary.Api.UrlImgUp
                .Secure()
                .Signed(true)
                .BuildUrl(publicId);
        }

        var httpClient = _httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync(urlFirmada, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var tipoContenido = response.Content.Headers.ContentType?.MediaType
            ?? (esPdf ? "application/pdf" : "image/jpeg");

        return new ComprobanteArchivoDescriptor(stream, tipoContenido);
    }

    public void EliminarComprobante(string? urlRelativaOrAbsoluta)
    {
        if (string.IsNullOrWhiteSpace(urlRelativaOrAbsoluta)) return;

        try
        {
            var (publicId, esPdf) = ExtraerPublicId(urlRelativaOrAbsoluta);

            if (!string.IsNullOrEmpty(publicId))
            {
                if (esPdf)
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

    private static (string PublicId, bool EsPdf) ExtraerPublicId(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath; // ej: /aerof1gd/raw/upload/v1786021379/comprobantes_comite/Comprobante_abc.pdf

            var esPdf = path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

            var indexFolder = path.IndexOf("comprobantes_comite/", StringComparison.OrdinalIgnoreCase);
            if (indexFolder < 0) return (string.Empty, esPdf);

            var pathFromFolder = path.Substring(indexFolder); // comprobantes_comite/Comprobante_abc.pdf

            if (esPdf)
            {
                // Los archivos RAW (PDF) requieren mantener el ".pdf" en su PublicID
                return (pathFromFolder, esPdf);
            }

            // Las imágenes NO llevan la extensión en el PublicID
            var extensionIndex = pathFromFolder.LastIndexOf('.');
            var publicId = extensionIndex > 0 ? pathFromFolder.Substring(0, extensionIndex) : pathFromFolder;
            return (publicId, esPdf);
        }
        catch
        {
            return (string.Empty, false);
        }
    }
}
