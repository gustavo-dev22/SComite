using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Application.Actividades.Dtos;
using AulaComite.Application.Common.Interfaces;
using Dapper;

namespace AulaComite.Infrastructure.Repositories
{
    public class ActividadRepository : IActividadRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ActividadRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<ActividadComiteDTO>> ObtenerPorAulaAsync(int aulaId, int anioLectivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<ActividadComiteDTO>(
                "sp_Actividades_ListarPorAula",
                new { AulaId = aulaId, AnioLectivo = anioLectivo },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> GuardarAsync(int id, int aulaId, string nombreActividad, string? descripcion, DateTime fechaProgramada, decimal montoPresupuestado, decimal cuotaSugeridaPorAlumno, string estado)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(
                "sp_Actividades_Guardar",
                new
                {
                    Id = id,
                    AulaId = aulaId,
                    NombreActividad = nombreActividad,
                    Descripcion = descripcion,
                    FechaProgramada = fechaProgramada,
                    MontoPresupuestado = montoPresupuestado,
                    CuotaSugeridaPorAlumno = cuotaSugeridaPorAlumno,
                    Estado = estado
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> EliminarAsync(int id, int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteScalarAsync<int>(
                "sp_Actividades_Eliminar",
                new { Id = id, AulaId = aulaId },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }
    }
}
