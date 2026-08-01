using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IActaAsambleaRepository
    {
        Task<IEnumerable<ActaAsambleaComite>> ObtenerPorAulaAsync(int aulaId, int anioLectivo);
        Task<int> GuardarAsync(int id, int aulaId, string numeroActa, string titulo, DateTime fechaReunion, string agendaAcuerdos, string estadoActa, string? urlDocumentoPdf, string usuarioRegistro);
        Task<bool> EliminarAsync(int id, int aulaId);
        Task<string> ObtenerSiguienteNumeroActaAsync(int aulaId, int anioLectivo);
    }
}
