namespace RecurringThings.Repository;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a transaction context for database operations.
/// </summary>
/// <remarks>
/// <para>
/// Transaction contexts are created by database-specific transaction managers
/// and passed to repository methods to ensure operations execute within a transaction.
/// </para>
/// <para>
/// When null is passed to repository methods, operations execute without a transaction.
/// </para>
/// </remarks>
public interface ITransactionContext : IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous commit operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous rollback operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
