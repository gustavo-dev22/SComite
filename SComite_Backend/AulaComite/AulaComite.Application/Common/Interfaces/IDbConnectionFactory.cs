using System;
using System.Data;
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
        /// </summary>
        Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action);

        /// <summary>
        /// Igual que <see cref="ExecuteInTransactionAsync(Func{IDbConnection, IDbTransaction, Task})"/>
        /// pero permitiendo devolver un resultado.
        /// </summary>
        Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action);
    }
}