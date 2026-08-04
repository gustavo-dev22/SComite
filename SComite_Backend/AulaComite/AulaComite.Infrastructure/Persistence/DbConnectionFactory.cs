using AulaComite.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
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

        public async Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action)
        {
            await using var connection = CreateConnection() as SqlConnection
                ?? throw new InvalidOperationException("La conexión creada no es compatible con SQL Server.");
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                await action(connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action)
        {
            await using var connection = CreateConnection() as SqlConnection
                ?? throw new InvalidOperationException("La conexión creada no es compatible con SQL Server.");
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                var result = await action(connection, transaction);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}