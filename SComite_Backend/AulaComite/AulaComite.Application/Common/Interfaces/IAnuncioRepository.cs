using AulaComite.Application.Comite.Dtos;
using AulaComite.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IAnuncioRepository
    {
        Task<IEnumerable<AnuncioComite>> ObtenerPorAulaAsync(int aulaId, int anioLectivo);
        Task<int> GuardarAsync(int id, int aulaId, string titulo, string contenido, string categoria, bool esFijado, string? urlAdjunto, string usuarioRegistro);
        Task<bool> EliminarAsync(int id, int aulaId);
        Task<IEnumerable<AuditoriaLecturaDto>> ObtenerAuditoriaLecturasAsync(int anuncioId);
    }
}
