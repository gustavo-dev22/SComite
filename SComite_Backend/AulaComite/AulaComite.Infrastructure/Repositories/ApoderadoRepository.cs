using AulaComite.Application.Apoderado.Dtos;
using AulaComite.Application.Common.Interfaces;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AulaComite.Infrastructure.Repositories
{
    public class ApoderadoRepository : IApoderadoRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ApoderadoRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<HijoApoderadoDto>> ObtenerHijosApoderadoAsync(string usuarioApoderado, int anioLectivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<HijoApoderadoDto>(
                "sp_Apoderado_ObtenerHijos",
                new { UsuarioApoderado = usuarioApoderado, AnioLectivo = anioLectivo },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<CuotaApoderadoDto>> ObtenerCuotasPendientesAsync(int estudianteId, int anioLectivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<CuotaApoderadoDto>(
                "sp_Apoderado_ObtenerCuotasPendientes",
                new { EstudianteId = estudianteId, AnioLectivo = anioLectivo },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<AnuncioApoderadoDto>> ObtenerAnunciosMuroAsync(int estudianteId, int anioLectivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<AnuncioApoderadoDto>(
                "sp_Apoderado_ObtenerAnunciosMuro",
                new { EstudianteId = estudianteId, AnioLectivo = anioLectivo },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task RegistrarLecturaAnuncioAsync(int anuncioId, int estudianteId, string usuarioApoderado)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                "sp_Apoderado_RegistrarLecturaAnuncio",
                new { AnuncioId = anuncioId, EstudianteId = estudianteId, UsuarioApoderado = usuarioApoderado },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<EventoCronogramaApoderadoDto>> ObtenerCronogramaEventosAsync(int estudianteId, int anioLectivo)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<EventoCronogramaApoderadoDto>(
                "sp_Apoderado_ObtenerCronogramaEventos",
                new { EstudianteId = estudianteId, AnioLectivo = anioLectivo },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
