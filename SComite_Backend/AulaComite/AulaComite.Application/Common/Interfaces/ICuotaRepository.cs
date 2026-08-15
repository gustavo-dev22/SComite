using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using AulaComite.Application.Cuotas.Dtos;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface ICuotaRepository
    {
        Task<int> CrearCuotaMasivaAsync(Cuota cuota, IDbTransaction? transaction = null);
        Task<IEnumerable<CuotaDto>> ObtenerPorAulaAsync(int aulaId);
        Task GenerarProgramacionMensualAsync(int aulaId, string conceptoBase, decimal montoMensual, int mesInicio, int diaVencimiento, int anioLectivo, IDbTransaction? transaction = null);
        Task<IEnumerable<CuotaEstudianteCobro>> ObtenerDetalleCobroEstudiantesAsync(int cuotaId);
        Task RegistrarPagoManualAsync(int cuotaDetalleId, decimal montoAbonado, string formaPago, IDbTransaction? transaction = null);
        Task AnularPagoEstudianteAsync(int cuotaDetalleId, IDbTransaction? transaction = null);
        Task<IEnumerable<EstudiantePendienteCuotaDto>> ObtenerEstudiantesPendientesAsync(int cuotaId);
        Task<int?> ObtenerAulaIdPorCuotaDetalleAsync(int cuotaDetalleId);
        Task<int?> ObtenerAulaIdPorCuotaAsync(int cuotaId);
        Task<string?> ObtenerEstadoCuotaPorCuotaDetalleAsync(int cuotaDetalleId);
        Task<CuotaDetalleInfoDto?> ObtenerDetalleCobroInfoAsync(int cuotaDetalleId);
        Task<bool> CambiarEstadoExoneracionAsync(int cuotaDetalleId, string nuevoEstado, string? motivo);
        Task<IEnumerable<EstudianteExoneradoCuotaDto>> ObtenerEstudiantesExoneradosAsync(int cuotaId);
        Task<bool> CambiarEstadoCuotaAsync(int cuotaId, string nuevoEstado);
    }
}
