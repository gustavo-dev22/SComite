using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface ILogRepository
    {
        Task RegistrarAsync(string nivel, string modulo, string accion, string mensaje, string? usuario = null, string? ip = null, string? exception = null, IDbTransaction? transaction = null);
        Task<PagedResultDto<LogSistema>> ObtenerFiltradosAsync(DateTime? fechaInicio, DateTime? fechaFin, string? nivel, string? modulo, string? busqueda, int pagina, int tamanoPagina);
    }
}
