using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Domain.Entities;

namespace AulaComite.Application.Common.Interfaces
{
    public interface ICuotaRepository
    {
        Task<int> CrearCuotaMasivaAsync(Cuota cuota);
        Task<IEnumerable<Cuota>> ObtenerPorAulaAsync(int aulaId);
        Task GenerarProgramacionMensualAsync(int aulaId, string conceptoBase, decimal montoMensual, int mesInicio, int diaVencimiento, int anioLectivo);
        Task<IEnumerable<CuotaEstudianteCobro>> ObtenerDetalleCobroEstudiantesAsync(int cuotaId);
        Task RegistrarPagoManualAsync(int cuotaDetalleId, decimal montoAbonado, string formaPago);
        Task AnularPagoEstudianteAsync(int cuotaDetalleId);
    }
}
