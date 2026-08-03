using AulaComite.Application.Apoderado.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IApoderadoRepository
    {
        Task<IEnumerable<HijoApoderadoDto>> ObtenerHijosApoderadoAsync(string usuarioApoderado, int anioLectivo);
        Task<IEnumerable<CuotaApoderadoDto>> ObtenerCuotasPendientesAsync(int estudianteId, int anioLectivo);
        Task<IEnumerable<AnuncioApoderadoDto>> ObtenerAnunciosMuroAsync(int estudianteId, int anioLectivo);
        Task RegistrarLecturaAnuncioAsync(int anuncioId, int estudianteId, string usuarioApoderado);
        Task<IEnumerable<EventoCronogramaApoderadoDto>> ObtenerCronogramaEventosAsync(int estudianteId, int anioLectivo);
        Task<IEnumerable<ActaApoderadoDto>> ObtenerActasAprobadasAsync(int estudianteId, int anioLectivo);
    }
}
