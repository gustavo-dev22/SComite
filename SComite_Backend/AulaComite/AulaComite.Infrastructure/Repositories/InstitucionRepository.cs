using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using Dapper;

namespace AulaComite.Infrastructure.Repositories
{
    public class InstitucionRepository : IInstitucionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public InstitucionRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<InstitucionEducativa?> ObtenerAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<InstitucionEducativa>(
                "sp_InstitucionEducativa_Obtener",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> GuardarAsync(InstitucionEducativa entidad)
        {
            using var connection = _connectionFactory.CreateConnection();
            var fechaActualizada = await connection.QueryFirstOrDefaultAsync<DateTime?>(
                "sp_InstitucionEducativa_Guardar",
                new
                {
                    entidad.NombreInstitucion,
                    entidad.Direccion,
                    entidad.UrlLogo,
                    entidad.UsuarioActualizacion
                },
                commandType: CommandType.StoredProcedure
            );

            if (fechaActualizada.HasValue)
            {
                entidad.FechaActualizacion = fechaActualizada.Value;
            }

            return true;
        }
    }
}
