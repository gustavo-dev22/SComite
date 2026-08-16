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

        public async Task<IEnumerable<CuotaDto>> ObtenerPorAulaAsync(int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<CuotaDto>(
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

        public async Task RegistrarPagoManualAsync(int cuotaDetalleId, decimal montoAbonado, string formaPago, string? usuarioRegistro = null, IDbTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? _connectionFactory.CreateConnection();
            try
            {
                await connection.ExecuteAsync(
                    "sp_Cuotas_RegistrarPagoManual",
                    new { CuotaDetalleId = cuotaDetalleId, MontoAbonado = montoAbonado, FormaPago = formaPago, UsuarioRegistro = usuarioRegistro },
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

        public async Task<int?> ObtenerAulaIdPorCuotaDetalleAsync(int cuotaDetalleId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT c.AulaId
                FROM CuotaDetalleEstudiante cd
                INNER JOIN Cuotas c ON c.Id = cd.CuotaId
                WHERE cd.Id = @CuotaDetalleId";
            return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { CuotaDetalleId = cuotaDetalleId });
        }

        public async Task<int?> ObtenerAulaIdPorCuotaAsync(int cuotaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT AulaId FROM Cuotas WHERE Id = @CuotaId";
            return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { CuotaId = cuotaId });
        }

        public async Task<string?> ObtenerEstadoCuotaPorCuotaDetalleAsync(int cuotaDetalleId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT c.Estado
                FROM CuotaDetalleEstudiante cd
                INNER JOIN Cuotas c ON c.Id = cd.CuotaId
                WHERE cd.Id = @CuotaDetalleId";
            return await connection.QueryFirstOrDefaultAsync<string>(sql, new { CuotaDetalleId = cuotaDetalleId });
        }

        public async Task<CuotaDetalleInfoDto?> ObtenerDetalleCobroInfoAsync(int cuotaDetalleId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT c.Concepto,
                       (e.ApellidoPaterno + ' ' + e.ApellidoMaterno + ', ' + e.Nombres) AS EstudianteNombreCompleto,
                       cd.MontoAsignado,
                       cd.MontoPagado
                FROM CuotaDetalleEstudiante cd
                INNER JOIN Cuotas c ON c.Id = cd.CuotaId
                INNER JOIN Estudiantes e ON cd.EstudianteId = e.Id
                WHERE cd.Id = @CuotaDetalleId";
            return await connection.QueryFirstOrDefaultAsync<CuotaDetalleInfoDto>(sql, new { CuotaDetalleId = cuotaDetalleId });
        }

        public async Task<bool> CambiarEstadoExoneracionAsync(int cuotaDetalleId, string nuevoEstado, string? motivo)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                        UPDATE CuotaDetalleEstudiante
                        SET EstadoPago = @NuevoEstado,
                            MotivoExoneracion = @Motivo,
                            FechaModificacionEstado = DATEADD(HOUR, -5, GETUTCDATE())
                        WHERE Id = @Id;";

            var filasAfectadas = await connection.ExecuteAsync(sql, new
            {
                Id = cuotaDetalleId,
                NuevoEstado = nuevoEstado.ToUpper(),
                Motivo = motivo
            });

            return filasAfectadas > 0;
        }

        public async Task<IEnumerable<EstudianteExoneradoCuotaDto>> ObtenerEstudiantesExoneradosAsync(int cuotaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<EstudianteExoneradoCuotaDto>(
                "sp_Cuotas_ObtenerEstudiantesExonerados",
                new { CuotaId = cuotaId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> CambiarEstadoCuotaAsync(int cuotaId, string nuevoEstado)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                        UPDATE Cuotas
                        SET Estado = @NuevoEstado,
                            FechaCierre = CASE WHEN @NuevoEstado = 'CERRADA' THEN DATEADD(HOUR, -5, GETUTCDATE()) ELSE NULL END
                        WHERE Id = @CuotaId;";

            var filasAfectadas = await connection.ExecuteAsync(sql, new
            {
                CuotaId = cuotaId,
                NuevoEstado = nuevoEstado.ToUpper()
            });

            return filasAfectadas > 0;
        }
    }
}
