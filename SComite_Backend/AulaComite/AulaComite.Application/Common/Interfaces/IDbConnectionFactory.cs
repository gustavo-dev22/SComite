using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();

        /// <summary>
        /// Ejecuta una operación compuesta dentro de una transacción atómica,
        /// reutilizando una única conexión abierta para toda la operación.
        /// Si el delegado lanza una excepción, se hace rollback; si no, commit.
        /// La transacción se inicia de forma asíncrona con <c>BeginTransactionAsync</c>.
        /// </summary>
        Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Igual que <see cref="ExecuteInTransactionAsync(Func{IDbConnection, IDbTransaction, Task}, CancellationToken)"/>
        /// pero permitiendo devolver un resultado.
        /// </summary>
        Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action, CancellationToken cancellationToken = default);
    }
}