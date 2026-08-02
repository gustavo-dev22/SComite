using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Auditoria.Dtos;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IAuditoriaRepository
    {
        Task<IEnumerable<ResumenCajaAulaDto>> ObtenerResumenGeneralCajasAsync(int anioLectivo, string? nivel);
    }
}
