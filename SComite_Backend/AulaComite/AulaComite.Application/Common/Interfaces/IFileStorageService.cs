using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> GuardarComprobanteAsync(byte[] contenido, string nombreOriginal, CancellationToken cancellationToken = default);
        void EliminarComprobante(string? urlRelativa);
    }
}
