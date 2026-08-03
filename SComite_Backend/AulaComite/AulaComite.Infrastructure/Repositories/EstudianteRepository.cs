using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using Dapper;
using System.Data;
using AulaComite.Application.Estudiantes.Dtos;

namespace AulaComite.Infrastructure.Repositories
{
    public class EstudianteRepository : IEstudianteRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public EstudianteRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Estudiante>> ObtenerPorAulaAsync(int aulaId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<Estudiante>(
                "sp_Estudiantes_ObtenerPorAula",
                new { AulaId = aulaId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> CrearEstudianteAsync(Estudiante e)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(
                "sp_Estudiantes_Crear",
                new
                {
                    AulaId = e.AulaId,
                    TipoDocumento = e.TipoDocumento,
                    NumeroDocumento = e.NumeroDocumento,
                    Nombres = e.Nombres,
                    ApellidoPaterno = e.ApellidoPaterno,
                    ApellidoMaterno = e.ApellidoMaterno,
                    UsuarioIdApoderadoSasi = e.UsuarioIdApoderadoSasi,
                    NombreApoderado = e.NombreApoderado,
                    TelefonoApoderado = e.TelefonoApoderado
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> ActualizarEstudianteAsync(Estudiante e)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "sp_Estudiantes_Actualizar",
                new
                {
                    Id = e.Id,
                    AulaId = e.AulaId,
                    TipoDocumento = e.TipoDocumento,
                    NumeroDocumento = e.NumeroDocumento,
                    Nombres = e.Nombres,
                    ApellidoPaterno = e.ApellidoPaterno,
                    ApellidoMaterno = e.ApellidoMaterno,
                    UsuarioIdApoderadoSasi = e.UsuarioIdApoderadoSasi,
                    NombreApoderado = e.NombreApoderado,
                    TelefonoApoderado = e.TelefonoApoderado
                },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }

        public async Task<bool> EliminarEstudianteLogicoAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "sp_Estudiantes_EliminarLogico",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }

        public async Task<int> CargaMasivaEstudiantesAsync(int aulaId, IEnumerable<EstudianteImportacionItemDto> listaEstudiantes)
        {
            using var connection = _connectionFactory.CreateConnection();

            var dt = new DataTable();
            dt.Columns.Add("TipoDocumento", typeof(string));
            dt.Columns.Add("NumeroDocumento", typeof(string));
            dt.Columns.Add("Nombres", typeof(string));
            dt.Columns.Add("ApellidoPaterno", typeof(string));
            dt.Columns.Add("ApellidoMaterno", typeof(string));
            dt.Columns.Add("UsuarioIdApoderadoSasi", typeof(string));
            dt.Columns.Add("NombreApoderado", typeof(string));
            dt.Columns.Add("TelefonoApoderado", typeof(string));

            foreach (var item in listaEstudiantes)
            {
                dt.Rows.Add(
                    item.TipoDocumento,
                    item.NumeroDocumento,
                    item.Nombres,
                    item.ApellidoPaterno,
                    item.ApellidoMaterno,
                    item.UsuarioIdApoderadoSasi ?? (object)DBNull.Value,
                    item.NombreApoderado ?? (object)DBNull.Value,
                    item.TelefonoApoderado ?? (object)DBNull.Value
                );
            }

            var parameters = new DynamicParameters();
            parameters.Add("@AulaId", aulaId);
            parameters.Add("@Estudiantes", dt.AsTableValuedParameter("EstudianteCargaMasivaType"));

            return await connection.ExecuteScalarAsync<int>(
                "sp_Estudiantes_CargaMasiva",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
