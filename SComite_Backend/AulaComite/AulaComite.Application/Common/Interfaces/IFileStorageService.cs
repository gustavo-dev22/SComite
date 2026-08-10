using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AulaComite.Application.Common.Interfaces
{
    public sealed record ComprobanteArchivoDescriptor(Stream Contenido, string? TipoContenido);

    public interface IFileStorageService
    {
        /// <summary>
        /// Guarda el comprobante transmitiendo el <see cref="Stream"/> de forma directa
        /// (sin cargar buffers enteros en memoria) y devuelve el identificador/URL que
        /// debe almacenarse en base de datos y usarse para su posterior visualización.
        /// </summary>
        Task<string> GuardarComprobanteAsync(Stream contenido, string nombreOriginal, CancellationToken cancellationToken = default);

        /// <summary>
        /// Devuelve el contenido del comprobante para servirlo a través de un endpoint
        /// protegido con [Authorize]. Devuelve null si el recurso no existe.
        /// </summary>
        Task<ComprobanteArchivoDescriptor?> ObtenerComprobanteAsync(string urlOIdentificador, CancellationToken cancellationToken = default);

        void EliminarComprobante(string? urlRelativa);
    }
}
