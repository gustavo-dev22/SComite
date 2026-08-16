using AulaComite.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace AulaComite.Infrastructure.Persistence
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly SqlRetryLogicBaseProvider _retryProvider;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;

            // 🛡️ T3.1: Reintento automático ante fallas transitorias de red/BD con backoff exponencial.
            _retryProvider = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new SqlRetryLogicOption
            {
                NumberOfTries = 4,
                DeltaTime = TimeSpan.FromSeconds(3),
                MaxTimeInterval = TimeSpan.FromSeconds(20),
                TransientErrors = new List<int>
                {
                    // Errores transitorios clásicos de SQL Server / Azure SQL
                    1205,  // Deadlock (víctima elegida)
                    4060,  // No se puede abrir la base de datos
                    40197, // Error transitorio del servicio
                    40501, // Servicio ocupado
                    40613, // Base de datos no disponible (carga)
                    49918, // No se puede procesar solicitud (recursos insuficientes)
                    49919, // Límite de operaciones simultáneas
                    49920, // Operación en curso
                    11001  // Host no encontrado
                }
            });
        }

        public IDbConnection CreateConnection()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            // 🛡️ T3.1: Command Timeout de 30s por defecto si no fue configurado explícitamente.
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (!builder.ContainsKey("Command Timeout") || builder.CommandTimeout <= 0)
                builder.CommandTimeout = 30;

            var connection = new SqlConnection(builder.ConnectionString)
            {
                // Retry en la APERTURA de la conexión (fallas transitorias de red).
                RetryLogicProvider = _retryProvider
            };

            // Proxy que aplica el retry también a la EJECUCIÓN de comandos: Dapper crea
            // los SqlCommand internamente y el provider de la conexión solo cubre el Open.
            return new RetryCommandConnection(connection, _retryProvider);
        }

        public async Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action, CancellationToken cancellationToken = default)
        {
            await using var connection = ObtenerSqlConnection();
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
            await using var connection = ObtenerSqlConnection();
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

        private SqlConnection ObtenerSqlConnection()
        {
            var connection = CreateConnection();
            return connection is RetryCommandConnection proxy ? proxy.Inner : (SqlConnection)connection;
        }

        /// <summary>
        /// 🛡️ T3.1: Proxy de DbConnection que asigna el SqlRetryLogicProvider a cada
        /// comando creado (Dapper crea los SqlCommand internamente), habilitando el
        /// reintento automático en la ejecución de operaciones ante fallas transitorias.
        /// Hereda de DbConnection porque Dapper exige una conexión de ese tipo para sus
        /// operaciones asíncronas (no existe IDbConnectionAsync en .NET moderno).
        /// Nota: los proveedores internos no reintentan comandos dentro de transacciones.
        /// </summary>
        private sealed class RetryCommandConnection : DbConnection
        {
            private readonly SqlConnection _inner;
            private readonly SqlRetryLogicBaseProvider _retryProvider;

            public RetryCommandConnection(SqlConnection inner, SqlRetryLogicBaseProvider retryProvider)
            {
                _inner = inner;
                _retryProvider = retryProvider;
            }

            public SqlConnection Inner => _inner;

            #pragma warning disable CS8765
            public override string ConnectionString
            {
                get => _inner.ConnectionString;
                set => _inner.ConnectionString = value;
            }
#pragma warning restore CS8765

            public override int ConnectionTimeout => _inner.ConnectionTimeout;

            public override string Database => _inner.Database;

            public override string DataSource => _inner.DataSource;

            public override string ServerVersion => _inner.ServerVersion;

            public override ConnectionState State => _inner.State;

            protected override DbProviderFactory DbProviderFactory => SqlClientFactory.Instance;

            public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

            public override void Close() => _inner.Close();

            public override void Open() => _inner.Open();

            public override Task OpenAsync(CancellationToken cancellationToken) => _inner.OpenAsync(cancellationToken);

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
                _inner.BeginTransaction(isolationLevel);

            protected override DbCommand CreateDbCommand()
            {
                var command = _inner.CreateCommand();
                command.RetryLogicProvider = _retryProvider;
                return command;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}