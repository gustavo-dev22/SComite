using System;
using System.Collections.Generic;
using System.Text;
using AulaComite.Application.Common.Interfaces;
using AulaComite.Domain.Entities;
using Dapper;
using System.Data;

namespace AulaComite.Infrastructure.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUserContextService _userContextService;

        public LogRepository(IDbConnectionFactory connectionFactory, IUserContextService userContextService)
        {
            _connectionFactory = connectionFactory;
            _userContextService = userContextService;
        }

        public async Task RegistrarAsync(string nivel, string modulo, string accion, string mensaje, string? usuario = null, string? ip = null, string? exception = null, IDbTransaction? transaction = null)
        {
            // 🚀 Si no se especifica usuario e IP manualmente, los extraemos automáticamente del token/HTTP
            string usuarioFinal = !string.IsNullOrEmpty(usuario) ? usuario : _userContextService.ObtenerUsuarioActual();
            string ipFinal = !string.IsNullOrEmpty(ip) ? ip : _userContextService.ObtenerIpCliente();

            var connection = transaction?.Connection ?? _connectionFactory.CreateConnection();
            try
            {
                await connection.ExecuteAsync(
                    "sp_Logs_Registrar",
                    new
                    {
                        Nivel = nivel,
                        Modulo = modulo,
                        Accion = accion,
                        Usuario = usuarioFinal,
                        IP = ipFinal,
                        Mensaje = mensaje,
                        DetalleException = exception
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

        public async Task<PagedResultDto<LogSistema>> ObtenerFiltradosAsync(DateTime? fechaInicio, DateTime? fechaFin, string? nivel, string? modulo, string? busqueda, int pagina, int tamanoPagina)
        {
            using var connection = _connectionFactory.CreateConnection();
            var items = (await connection.QueryAsync<LogSistema>(
                "sp_Logs_ObtenerFiltrados",
                new
                {
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    Nivel = nivel,
                    Modulo = modulo,
                    Busqueda = busqueda,
                    Pagina = pagina,
                    TamanoPagina = tamanoPagina
                },
                commandType: CommandType.StoredProcedure
            )).ToList();

            int totalRegistros = items.FirstOrDefault()?.TotalRegistros ?? 0;
            int totalPaginas = (int)Math.Ceiling((double)totalRegistros / tamanoPagina);

            return new PagedResultDto<LogSistema>
            {
                Items = items,
                TotalRegistros = totalRegistros,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas > 0 ? totalPaginas : 1
            };
        }
    }
}
