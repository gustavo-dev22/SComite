using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using Dapper;
using System.Data;
using AulaComite.Application.Cuotas.Dtos;

namespace AulaComite.Infrastructure.Repositories
{
    public class CuotaRepository : ICuotaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CuotaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CrearCuotaMasivaAsync(Cuota cuota, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? _connectionFactory.CreateConnection();
            try
            {
                return await connection.ExecuteScalarAsync<int>(
                    "sp_Cuotas_Crear",
                    new
                    {
                        cuota.AulaId,
                        cuota.Concepto,
                        cuota.MontoIndividual,
                        cuota.FechaVencimiento,
                        cuota.Observacion,
                        cuota.ActividadId
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure
                );
            }
            finally
            {
                if (transaction == null) connection.Dispose();
            }
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

        public async Task GenerarProgramacionMensualAsync(int aulaId, string conceptoBase, decimal montoMensual, int mesInicio, int diaVencimiento, int anioLectivo, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? _connectionFactory.CreateConnection();
            try
            {
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
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure
                );
            }
            finally
            {
                if (transaction == null) connection.Dispose();
            }
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

        public async Task RegistrarPagoManualAsync(int cuotaDetalleId, decimal montoAbonado, string formaPago, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? _connectionFactory.CreateConnection();
            try
            {
                await connection.ExecuteAsync(
                    "sp_Cuotas_RegistrarPagoManual",
                    new { CuotaDetalleId = cuotaDetalleId, MontoAbonado = montoAbonado, FormaPago = formaPago },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure
                );
            }
            finally
            {
                if (transaction == null) connection.Dispose();
            }
        }

        public async Task AnularPagoEstudianteAsync(int cuotaDetalleId, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? _connectionFactory.CreateConnection();
            try
            {
                await connection.ExecuteAsync(
                    "sp_Cuotas_AnularPagoEstudiante",
                    new { CuotaDetalleId = cuotaDetalleId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure
                );
            }
            finally
            {
                if (transaction == null) connection.Dispose();
            }
        }

        public async Task<IEnumerable<EstudiantePendienteCuotaDto>> ObtenerEstudiantesPendientesAsync(int cuotaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<EstudiantePendienteCuotaDto>(
                "sp_Cuotas_ObtenerEstudiantesPendientes",
                new { CuotaId = cuotaId },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
