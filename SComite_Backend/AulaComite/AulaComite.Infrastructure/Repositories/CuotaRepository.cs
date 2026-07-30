using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using Dapper;
using System.Data;

namespace AulaComite.Infrastructure.Repositories
{
    public class CuotaRepository : ICuotaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CuotaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CrearCuotaMasivaAsync(Cuota cuota)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(
                "sp_Cuotas_Crear",
                new
                {
                    cuota.AulaId,
                    cuota.Concepto,
                    cuota.MontoIndividual,
                    cuota.FechaVencimiento,
                    cuota.Observacion
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<Cuota>> ObtenerPorAulaAsync(int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<Cuota>(
                "sp_Cuotas_ObtenerPorAula",
                new { AulaId = aulaId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task GenerarProgramacionMensualAsync(int aulaId, string conceptoBase, decimal montoMensual, int mesInicio, int diaVencimiento, int anioLectivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                "sp_Cuotas_GenerarProgramacionMensual",
                new
                {
                    AulaId = aulaId,
                    ConceptoBase = conceptoBase,
                    MontoMensual = montoMensual,
                    MesInicio = mesInicio,
                    DiaVencimiento = diaVencimiento,
                    AnioLectivo = anioLectivo
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<CuotaEstudianteCobro>> ObtenerDetalleCobroEstudiantesAsync(int cuotaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<CuotaEstudianteCobro>(
                "sp_Cuotas_ObtenerDetalleCobroEstudiantes",
                new { CuotaId = cuotaId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task RegistrarPagoManualAsync(int cuotaDetalleId, decimal montoAbonado, string formaPago)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                "sp_Cuotas_RegistrarPagoManual",
                new { CuotaDetalleId = cuotaDetalleId, MontoAbonado = montoAbonado, FormaPago = formaPago },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task AnularPagoEstudianteAsync(int cuotaDetalleId)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                "sp_Cuotas_AnularPagoEstudiante",
                new { CuotaDetalleId = cuotaDetalleId },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
