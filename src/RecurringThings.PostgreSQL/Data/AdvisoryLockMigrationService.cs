namespace RecurringThings.PostgreSQL.Data;

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Service that applies EF Core migrations with PostgreSQL advisory lock protection.
/// </summary>
/// <remarks>
/// <para>
/// Uses PostgreSQL advisory locks to prevent race conditions when multiple application
/// replicas start simultaneously and attempt to apply migrations.
/// </para>
/// <para>
/// The advisory lock is session-level and automatically released when the connection closes.
/// </para>
/// </remarks>
internal sealed class AdvisoryLockMigrationService
{
    // Lock ID computed from "RecurringThings" - ensures uniqueness across applications
    // This is a hash of the string to get a consistent long value
    private const long LockId = 0x526563757272696E; // "Recurrin" as ASCII bytes

    private readonly RecurringThingsDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvisoryLockMigrationService"/> class.
    /// </summary>
    /// <param name="context">The DbContext to migrate.</param>
    public AdvisoryLockMigrationService(RecurringThingsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Applies pending migrations with advisory lock protection.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous migration operation.</returns>
    /// <remarks>
    /// <para>
    /// This method acquires a PostgreSQL advisory lock before applying migrations,
    /// ensuring that only one instance can apply migrations at a time.
    /// </para>
    /// <para>
    /// If another instance holds the lock, this method will block until the lock
    /// is released (which happens when migrations complete).
    /// </para>
    /// </remarks>
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Acquire advisory lock (blocks until lock is available)
            await AcquireAdvisoryLockAsync(connection, cancellationToken).ConfigureAwait(false);

            try
            {
                // Apply pending migrations
                await _context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // Release advisory lock
                await ReleaseAdvisoryLockAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            await connection.CloseAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Applies pending migrations synchronously with advisory lock protection.
    /// </summary>
    /// <remarks>
    /// Used during application startup when async may not be available.
    /// </remarks>
    public void Migrate()
    {
        var connection = _context.Database.GetDbConnection();
        connection.Open();

        try
        {
            // Acquire advisory lock (blocks until lock is available)
            AcquireAdvisoryLock(connection);

            try
            {
                // Apply pending migrations
                _context.Database.Migrate();
            }
            finally
            {
                // Release advisory lock
                ReleaseAdvisoryLock(connection);
            }
        }
        finally
        {
            connection.Close();
        }
    }

    private static async Task AcquireAdvisoryLockAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_lock({LockId})";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReleaseAdvisoryLockAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_unlock({LockId})";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AcquireAdvisoryLock(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_lock({LockId})";
        command.ExecuteNonQuery();
    }

    private static void ReleaseAdvisoryLock(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_unlock({LockId})";
        command.ExecuteNonQuery();
    }
}
