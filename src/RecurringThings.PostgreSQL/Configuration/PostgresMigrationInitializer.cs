namespace RecurringThings.PostgreSQL.Configuration;

using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RecurringThings.PostgreSQL.Data;

/// <summary>
/// Initializer that runs PostgreSQL migrations on demand with thread-safe execution.
/// </summary>
/// <remarks>
/// <para>
/// This class is registered as a singleton and provides a thread-safe mechanism
/// to ensure migrations are run exactly once, regardless of how many times
/// <see cref="EnsureMigrated"/> is called.
/// </para>
/// <para>
/// Uses PostgreSQL advisory locks to ensure safe concurrent migration across
/// multiple application replicas.
/// </para>
/// </remarks>
/// <param name="lockService">The advisory lock service for distributed locking.</param>
/// <param name="options">The PostgreSQL options.</param>
internal sealed class PostgresMigrationInitializer(
    AdvisoryLockService lockService,
    IOptions<PostgreSqlOptions> options)
{
    // Lock ID computed from "RecurringThings_Migration" - ensures uniqueness
    // This is ASCII bytes of "RTMigrat" interpreted as a long
    private const long MigrationLockId = 0x52544D696772617; // "RTMigra" as ASCII

    private readonly Lock _lock = new();
    private volatile bool _migrated;

    /// <summary>
    /// Ensures that database migrations have been applied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is thread-safe and idempotent. It will only run migrations once,
    /// even if called from multiple threads simultaneously.
    /// </para>
    /// <para>
    /// If <see cref="PostgreSqlOptions.RunMigrationsOnStartup"/> is false, this method
    /// does nothing.
    /// </para>
    /// <para>
    /// Uses PostgreSQL advisory locks to coordinate with other application replicas,
    /// ensuring only one instance applies migrations at a time.
    /// </para>
    /// </remarks>
    public void EnsureMigrated()
    {
        if (_migrated || !options.Value.RunMigrationsOnStartup)
            return;

        lock (_lock)
        {
            if (_migrated)
                return;

            lockService.ExecuteWithLock(MigrationLockId, context =>
                context.Database.Migrate());

            _migrated = true;
        }
    }
}
