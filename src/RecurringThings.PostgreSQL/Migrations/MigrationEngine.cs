namespace RecurringThings.PostgreSQL.Migrations;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

/// <summary>
/// Manages database schema migrations for RecurringThings PostgreSQL storage.
/// </summary>
/// <remarks>
/// <para>
/// Migrations are embedded SQL scripts with naming convention: V###_Description.sql
/// (e.g., V001_InitialSchema.sql, V002_AddIndexes.sql).
/// </para>
/// <para>
/// The migration engine tracks applied versions in the __recurring_things_schema table.
/// Migrations are applied sequentially in version order within transactions.
/// </para>
/// </remarks>
public sealed partial class MigrationEngine
{
    private const string SchemaVersionTable = "__recurring_things_schema";
    private readonly string _connectionString;
    private static readonly object _migrationLock = new();
    private static bool _hasMigrated;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationEngine"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public MigrationEngine(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <summary>
    /// Ensures the database schema is up to date by running any pending migrations.
    /// </summary>
    /// <remarks>
    /// <para>This method is idempotent and safe to call multiple times.</para>
    /// <para>The database must already exist; this method will not create the database.</para>
    /// </remarks>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous migration operation.</returns>
    /// <exception cref="NpgsqlException">Thrown when a database error occurs.</exception>
    public async Task EnsureDatabaseMigratedAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: already migrated in this application instance
        if (_hasMigrated)
        {
            return;
        }

        // Lock to prevent concurrent migrations
        lock (_migrationLock)
        {
            if (_hasMigrated)
            {
                return;
            }

            // Run synchronously inside the lock to prevent race conditions
            EnsureDatabaseMigratedInternalAsync(cancellationToken).GetAwaiter().GetResult();
            _hasMigrated = true;
        }
    }

    /// <summary>
    /// Forces database migration regardless of cached state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method bypasses the static migration cache and always checks/applies pending migrations.
    /// Use this for integration tests where each test may use a different database.
    /// </para>
    /// <para>The database must already exist; this method will not create the database.</para>
    /// </remarks>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous migration operation.</returns>
    /// <exception cref="NpgsqlException">Thrown when a database error occurs.</exception>
    public async Task ForceMigrateAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseMigratedInternalAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureDatabaseMigratedInternalAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Ensure schema version table exists
        await EnsureSchemaVersionTableAsync(connection, cancellationToken).ConfigureAwait(false);

        // Get current version
        var currentVersion = await GetCurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);

        // Discover and sort pending migrations
        var migrations = DiscoverMigrations();
        var pendingMigrations = migrations.FindAll(m => m.Version > currentVersion);

        if (pendingMigrations.Count == 0)
        {
            return;
        }

        // Apply each pending migration in order
        foreach (var migration in pendingMigrations)
        {
            await ApplyMigrationAsync(connection, migration, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task EnsureSchemaVersionTableAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            CREATE TABLE IF NOT EXISTS {SchemaVersionTable} (
                version INTEGER NOT NULL PRIMARY KEY,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> GetCurrentVersionAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT COALESCE(MAX(version), 0) FROM {SchemaVersionTable}";

        await using var command = new NpgsqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return Convert.ToInt32(result);
    }

    private static List<MigrationScript> DiscoverMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        var migrations = new List<MigrationScript>();

        foreach (var resourceName in resourceNames)
        {
            // Look for embedded SQL scripts in the Migrations folder
            if (!resourceName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Extract the filename from the resource name
            var fileName = GetFileNameFromResourceName(resourceName);
            if (fileName is null)
            {
                continue;
            }

            // Parse version from filename (e.g., V001_InitialSchema.sql -> 1)
            var match = MigrationVersionRegex().Match(fileName);
            if (!match.Success)
            {
                continue;
            }

            var version = int.Parse(match.Groups["version"].Value);
            var content = ReadEmbeddedResource(assembly, resourceName);

            migrations.Add(new MigrationScript(version, resourceName, content));
        }

        migrations.Sort((a, b) => a.Version.CompareTo(b.Version));
        return migrations;
    }

    private static string? GetFileNameFromResourceName(string resourceName)
    {
        // Resource names are typically: Namespace.Folder.FileName.ext
        var parts = resourceName.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        // Get the last two parts (filename.ext)
        return $"{parts[^2]}.{parts[^1]}";
    }

    private static string ReadEmbeddedResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task ApplyMigrationAsync(
        NpgsqlConnection connection,
        MigrationScript migration,
        CancellationToken cancellationToken)
    {
        // Use a transaction for atomicity
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            // Execute the migration script
            await using var command = new NpgsqlCommand(migration.Content, connection, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    [GeneratedRegex(@"^V(?<version>\d+)_", RegexOptions.IgnoreCase)]
    private static partial Regex MigrationVersionRegex();

    private sealed record MigrationScript(int Version, string ResourceName, string Content);
}
