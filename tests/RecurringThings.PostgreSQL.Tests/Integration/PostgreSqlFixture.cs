namespace RecurringThings.PostgreSQL.Tests.Integration;

using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RecurringThings.Configuration;
using RecurringThings.Engine;
using RecurringThings.PostgreSQL.Configuration;

/// <summary>
/// Fixture that provides a PostgreSQL-backed IServiceProvider for integration tests.
/// </summary>
/// <remarks>
/// <para>
/// The Provider property is null if POSTGRES_CONNECTION_STRING is not set.
/// Tests should check for null and skip accordingly.
/// </para>
/// <para>
/// Creates a unique database per test run and drops it on disposal.
/// The POSTGRES_CONNECTION_STRING must point to a server where the test can create/drop databases.
/// </para>
/// </remarks>
public sealed class PostgreSqlFixture : IAsyncDisposable
{
    private readonly string? _baseConnectionString;
    private readonly string _testDatabaseName;
    private string? _testConnectionString;
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the service provider, or null if POSTGRES_CONNECTION_STRING is not set.
    /// </summary>
    public IServiceProvider? Provider => _serviceProvider;

    public PostgreSqlFixture()
    {
        _baseConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        // PostgreSQL limit is 63 chars
        // Format: rt_test_<guid> to ensure uniqueness for parallel execution
        // "rt_test_" (8 chars) + guid (32 chars) = 40 chars total, well under limit
        _testDatabaseName = $"rt_test_{Guid.NewGuid():N}";

        if (!string.IsNullOrEmpty(_baseConnectionString))
        {
            // Note: xUnit calls constructor synchronously, but we need async for DB creation
            // We use Task.Run().GetAwaiter().GetResult() as fixture creation pattern
            InitializeAsync().GetAwaiter().GetResult();
        }
    }

    private async Task InitializeAsync()
    {
        // Step 1: Create the test database using the base connection string
        // The base connection points to an existing database (e.g., 'postgres')
        await using (var connection = new NpgsqlConnection(_baseConnectionString))
        {
            await connection.OpenAsync();
            await using var createDbCommand = new NpgsqlCommand(
                $"CREATE DATABASE \"{_testDatabaseName}\"", connection);
            await createDbCommand.ExecuteNonQueryAsync();
        }

        // Step 2: Build connection string for the new test database
        var builder = new NpgsqlConnectionStringBuilder(_baseConnectionString)
        {
            Database = _testDatabaseName
        };
        _testConnectionString = builder.ConnectionString;

        // Step 3: Register services pointing to the test database
        var services = new ServiceCollection();
        services.AddRecurringThings(rtBuilder =>
            rtBuilder.UsePostgreSql((provider, options) =>
            {
                options.ConnectionString = _testConnectionString;
                options.RunMigrationsOnStartup = true;
            }));

        _serviceProvider = services.BuildServiceProvider();

        // Step 4: Trigger migrations by resolving the engine
        _ = _serviceProvider.GetRequiredService<IRecurrenceEngine>();
    }

    public async ValueTask DisposeAsync()
    {
        if (string.IsNullOrEmpty(_baseConnectionString))
        {
            return;
        }

        // Step 1: Dispose service provider to release all connections
        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync();
        }

        // Step 2: Force close any remaining connections and drop the database
        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync();

        // Terminate all connections to the test database
        await using (var terminateCommand = new NpgsqlCommand($"""
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = '{_testDatabaseName}'
            AND pid <> pg_backend_pid()
            """, connection))
        {
            await terminateCommand.ExecuteNonQueryAsync();
        }

        // Drop the database
        await using var dropDbCommand = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{_testDatabaseName}\"", connection);
        await dropDbCommand.ExecuteNonQueryAsync();
    }
}
