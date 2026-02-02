namespace RecurringThings.PostgreSQL.Data;

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Service for executing operations within PostgreSQL advisory locks.
/// </summary>
/// <remarks>
/// <para>
/// Uses PostgreSQL advisory locks to provide distributed locking across multiple
/// application replicas. This is useful for coordinating exclusive access to
/// shared resources like database migrations.
/// </para>
/// <para>
/// The service manages the full connection lifecycle: creates a DbContext,
/// opens the connection, acquires the lock, executes the action, releases
/// the lock, and closes the connection.
/// </para>
/// <para>
/// The action receives the DbContext to ensure all database operations use
/// the same connection that holds the lock.
/// </para>
/// </remarks>
/// <param name="contextFactory">The DbContext factory for creating database contexts.</param>
internal sealed class AdvisoryLockService(IDbContextFactory<RecurringThingsDbContext> contextFactory)
{
    /// <summary>
    /// Executes an action within an advisory lock.
    /// </summary>
    /// <param name="lockId">The unique identifier for the lock.</param>
    /// <param name="action">The action to execute while holding the lock.</param>
    /// <remarks>
    /// The lock is acquired before executing the action and released afterward,
    /// even if the action throws an exception. The action receives the DbContext
    /// to ensure all database operations use the locked connection.
    /// </remarks>
    public void ExecuteWithLock(long lockId, Action<RecurringThingsDbContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var context = contextFactory.CreateDbContext();
        var connection = context.Database.GetDbConnection();
        connection.Open();

        try
        {
            AcquireLock(connection, lockId);
            try
            {
                action(context);
            }
            finally
            {
                ReleaseLock(connection, lockId);
            }
        }
        finally
        {
            connection.Close();
        }
    }

    /// <summary>
    /// Executes an asynchronous action within an advisory lock.
    /// </summary>
    /// <param name="lockId">The unique identifier for the lock.</param>
    /// <param name="action">The async action to execute while holding the lock.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// The lock is acquired before executing the action and released afterward,
    /// even if the action throws an exception. The action receives the DbContext
    /// to ensure all database operations use the locked connection.
    /// </remarks>
    public async Task ExecuteWithLockAsync(
        long lockId,
        Func<RecurringThingsDbContext, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await AcquireLockAsync(connection, lockId, cancellationToken).ConfigureAwait(false);
            try
            {
                await action(context).ConfigureAwait(false);
            }
            finally
            {
                await ReleaseLockAsync(connection, lockId, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            await connection.CloseAsync().ConfigureAwait(false);
        }
    }

    private static void AcquireLock(DbConnection connection, long lockId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_lock({lockId})";
        command.ExecuteNonQuery();
    }

    private static void ReleaseLock(DbConnection connection, long lockId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_unlock({lockId})";
        command.ExecuteNonQuery();
    }

    private static async Task AcquireLockAsync(
        DbConnection connection,
        long lockId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_lock({lockId})";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReleaseLockAsync(
        DbConnection connection,
        long lockId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_unlock({lockId})";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
