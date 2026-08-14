using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using Dapper;
using System.Data;

namespace AulaComite.Infrastructure.Repositories
{
    public class AulaRepository : IAulaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AulaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Aula>> ObtenerTodasAsync(int? periodoId)
        {
            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryAsync<Aula>(
                "sp_Aulas_ObtenerTodas",
                new { PeriodoId = periodoId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<Aula>> ObtenerAulasPorUsuarioAsync(int? periodoId, string? usuarioId, string? nombreCompleto)
        {
            using var connection = _connectionFactory.CreateConnection();

            // 🛡️ Devuelve SOLO las aulas a las que pertenece el usuario:
            //  - Comité de Aula: filas de ComiteIntegrantes (UsuarioIdSasi)
            //  - Apoderado: aulas de los hijos en Estudiantes (por UsuarioIdApoderadoSasi
            //    o por el nombre del apoderado, igual que sp_Apoderado_ObtenerHijos)
            const string sql = @"
                SELECT DISTINCT a.Id, a.PeriodoId, a.Nivel, a.Grado, a.Seccion,
                       (a.Nivel + ' - ' + a.Grado + ' ' + a.Seccion) AS NombreDisplay,
                       a.Estado, p.Anio AS AnioPeriodo
                FROM Aulas a
                INNER JOIN PeriodosLectivos p ON a.PeriodoId = p.Id
                WHERE (@PeriodoId IS NULL OR a.PeriodoId = @PeriodoId)
                  AND (
                      EXISTS (
                          SELECT 1 FROM ComiteIntegrantes ci
                          WHERE ci.AulaId = a.Id
                            AND ci.UsuarioIdSasi = @UsuarioId
                            AND ci.Estado = 1
                      )
                      OR EXISTS (
                          SELECT 1 FROM Estudiantes e
                          WHERE e.AulaId = a.Id
                            AND e.Estado = 1
                            AND (
                                LTRIM(RTRIM(ISNULL(e.UsuarioIdApoderadoSasi,''))) = @UsuarioId
                                OR (
                                    LTRIM(RTRIM(@NombreCompleto)) <> ''
                                    AND (
                                        LTRIM(RTRIM(ISNULL(e.NombreApoderado,''))) = @NombreCompleto
                                        OR LTRIM(RTRIM(ISNULL(e.NombreApoderado,''))) LIKE '%' + @NombreCompleto + '%'
                                    )
                                )
                            )
                      )
                  )
                ORDER BY a.Grado, a.Seccion, a.Id";

            return await connection.QueryAsync<Aula>(
                sql,
                new { PeriodoId = periodoId, UsuarioId = usuarioId, NombreCompleto = nombreCompleto }
            );
        }

        public async Task<IEnumerable<PeriodoLectivo>> ObtenerPeriodosAsync()
        {
            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryAsync<PeriodoLectivo>(
                "sp_Periodos_ObtenerTodos",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> CrearAulaAsync(Aula aula)
        {
            using var connection = _connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<int>(
                "sp_Aulas_Crear",
                new
                {
                    PeriodoId = aula.PeriodoId,
                    Nivel = aula.Nivel,
                    Grado = aula.Grado,
                    Seccion = aula.Seccion
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> ActualizarEstadoAulaAsync(int id, bool estado)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rows = await connection.ExecuteAsync(
                "sp_Aulas_ActualizarEstado",
                new { Id = id, Estado = estado },
                commandType: CommandType.StoredProcedure
            );

            return rows > 0;
        }

        public async Task<bool> ActualizarAulaAsync(Aula aula)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "sp_Aulas_Actualizar",
                new
                {
                    Id = aula.Id,
                    PeriodoId = aula.PeriodoId,
                    Nivel = aula.Nivel,
                    Grado = aula.Grado,
                    Seccion = aula.Seccion
                },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }

        public async Task<bool> EliminarAulaLogicoAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "sp_Aulas_EliminarLogico",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }

        public async Task<Aula?> ObtenerPorIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
            SELECT Id, PeriodoId, Nivel, Grado, Seccion 
            FROM Aulas
            WHERE Id = @Id";

            return await connection.QueryFirstOrDefaultAsync<Aula>(sql, new { Id = id });
        }
    }
}
