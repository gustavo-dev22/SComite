using AulaComite.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace AulaComite.Infrastructure.Persistence
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            return new SqlConnection(connectionString);
        }

        public async Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action, CancellationToken cancellationToken = default)
        {
            await using var connection = CreateConnection() as SqlConnection
                ?? throw new InvalidOperationException("La conexión creada no es compatible con SQL Server.");
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await action(connection, transaction);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action, CancellationToken cancellationToken = default)
        {
            await using var connection = CreateConnection() as SqlConnection
                ?? throw new InvalidOperationException("La conexión creada no es compatible con SQL Server.");
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action(connection, transaction);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}