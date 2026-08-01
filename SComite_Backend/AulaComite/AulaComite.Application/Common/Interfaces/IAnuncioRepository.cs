using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IAnuncioRepository
    {
        Task<IEnumerable<AnuncioComite>> ObtenerPorAulaAsync(int aulaId, int anioLectivo);
        Task<int> GuardarAsync(int id, int aulaId, string titulo, string contenido, string categoria, bool esFijado, string? urlAdjunto, string usuarioRegistro);
        Task<bool> EliminarAsync(int id, int aulaId);
    }
}
