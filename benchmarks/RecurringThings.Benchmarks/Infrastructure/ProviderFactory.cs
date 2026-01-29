namespace RecurringThings.Benchmarks.Infrastructure;

using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RecurringThings.Configuration;
using RecurringThings.Engine;
using RecurringThings.MongoDB.Configuration;
using RecurringThings.PostgreSQL.Configuration;

/// <summary>
/// Database provider types supported by benchmarks.
/// </summary>
public enum BenchmarkProvider
{
    /// <summary>MongoDB provider.</summary>
    MongoDB,

    /// <summary>PostgreSQL provider.</summary>
    PostgreSQL
}

/// <summary>
/// Factory for creating IRecurrenceEngine instances for each provider.
/// Uses volume-based database names so each data volume has its own persistent database.
/// </summary>
public static class ProviderFactory
{
    // Track which (provider, volume) combinations have been initialized this session
    private static readonly HashSet<(BenchmarkProvider, int)> InitializedDatabases = [];

    /// <summary>
    /// Gets the database name for a given provider and volume.
    /// </summary>
    public static string GetDatabaseName(BenchmarkProvider provider, int dataVolume) =>
        $"rt_bench_v{dataVolume}";

    /// <summary>
    /// Initializes the provider by running migrations/creating indexes for a specific volume.
    /// Called once per (provider, volume) combination.
    /// </summary>
    public static async Task InitializeProviderAsync(BenchmarkProvider provider, int dataVolume)
    {
        var key = (provider, dataVolume);
        if (InitializedDatabases.Contains(key))
            return;

        var dbName = GetDatabaseName(provider, dataVolume);
        Console.WriteLine($"Initializing {provider} database: {dbName}");

        switch (provider)
        {
            case BenchmarkProvider.MongoDB:
                // MongoDB index creation happens automatically on first use via IndexManager
                // Force it now by creating a temporary engine and making a query
                var mongoEngine = CreateEngine(provider, dataVolume);
                await foreach (var _ in mongoEngine.GetRecurrencesAsync(
                    "init", "init", DateTime.UtcNow, DateTime.UtcNow.AddDays(1)))
                {
                    break;
                }

                break;

            case BenchmarkProvider.PostgreSQL:
                // Create the benchmark database if it doesn't exist
                await EnsurePostgresDatabaseExistsAsync(dataVolume);

                // PostgreSQL migrations run via EF Core on first DbContext use
                var pgEngine = CreateEngine(provider, dataVolume);
                await foreach (var _ in pgEngine.GetRecurrencesAsync(
                    "init", "init", DateTime.UtcNow, DateTime.UtcNow.AddDays(1)))
                {
                    break;
                }

                break;
        }

        InitializedDatabases.Add(key);
        Console.WriteLine($"  {provider} initialization complete");
    }

    /// <summary>
    /// Creates a new IRecurrenceEngine instance for the specified provider and data volume.
    /// </summary>
    public static IRecurrenceEngine CreateEngine(BenchmarkProvider provider, int dataVolume)
    {
        var dbName = GetDatabaseName(provider, dataVolume);
        var services = new ServiceCollection();

        services.AddRecurringThings(builder =>
        {
            switch (provider)
            {
                case BenchmarkProvider.MongoDB:
                    builder.UseMongoDb(options =>
                    {
                        options.ConnectionString = GetMongoConnectionString();
                        options.DatabaseName = dbName;
                        options.CollectionName = "recurring_things";
                    });
                    break;

                case BenchmarkProvider.PostgreSQL:
                    builder.UsePostgreSql(options =>
                    {
                        options.ConnectionString = GetPostgresBenchmarkConnectionString(dataVolume);
                    });
                    break;
            }
        });

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IRecurrenceEngine>();
    }

    /// <summary>
    /// Checks if benchmark data is already seeded for the specified provider and volume.
    /// Returns true if the expected number of records exists.
    /// </summary>
    public static async Task<bool> IsDataSeededAsync(BenchmarkProvider provider, int dataVolume)
    {
        try
        {
            var engine = CreateEngine(provider, dataVolume);

            // Count records by querying with a wide date range
            var count = 0;
            await foreach (var _ in engine.GetRecurrencesAsync(
                DataSeeder.Organization,
                DataSeeder.ResourcePath,
                BenchmarkOptions.QueryStartUtc.AddYears(-1),
                BenchmarkOptions.QueryEndUtc.AddYears(1)))
            {
                count++;
                // If we find at least 90% of expected recurrences, consider it seeded
                // (90% of total is recurrences)
                if (count >= dataVolume * 0.9 * 0.9)
                    return true;
            }

            return false;
        }
        catch
        {
            // Database doesn't exist or other error - not seeded
            return false;
        }
    }

    /// <summary>
    /// Gets the MongoDB connection string from environment variable.
    /// </summary>
    public static string GetMongoConnectionString() =>
        Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
            ?? throw new InvalidOperationException("MONGODB_CONNECTION_STRING environment variable not set");

    /// <summary>
    /// Gets the PostgreSQL connection string from environment variable.
    /// </summary>
    public static string GetPostgresConnectionString() =>
        Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException("POSTGRES_CONNECTION_STRING environment variable not set");

    /// <summary>
    /// Checks if MongoDB is available (environment variable set).
    /// </summary>
    public static bool IsMongoAvailable() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING"));

    /// <summary>
    /// Checks if PostgreSQL is available (environment variable set).
    /// </summary>
    public static bool IsPostgresAvailable() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING"));

    /// <summary>
    /// Gets the list of available benchmark providers.
    /// </summary>
    public static IEnumerable<BenchmarkProvider> GetAvailableProviders()
    {
        if (IsMongoAvailable())
            yield return BenchmarkProvider.MongoDB;
        if (IsPostgresAvailable())
            yield return BenchmarkProvider.PostgreSQL;
    }

    /// <summary>
    /// Drops the benchmark database for the specified provider and volume.
    /// Use this for manual cleanup when needed.
    /// </summary>
    public static async Task DropDatabaseAsync(BenchmarkProvider provider, int dataVolume)
    {
        var dbName = GetDatabaseName(provider, dataVolume);
        Console.WriteLine($"Dropping {provider} database: {dbName}...");

        switch (provider)
        {
            case BenchmarkProvider.MongoDB:
                await DropMongoDatabaseAsync(dataVolume);
                break;

            case BenchmarkProvider.PostgreSQL:
                await DropPostgresDatabaseAsync(dataVolume);
                break;
        }

        // Remove from initialized set
        InitializedDatabases.Remove((provider, dataVolume));
        Console.WriteLine($"  {provider} database dropped");
    }

    private static async Task DropMongoDatabaseAsync(int dataVolume)
    {
        var dbName = GetDatabaseName(BenchmarkProvider.MongoDB, dataVolume);
        var connectionString = GetMongoConnectionString();
        var client = new MongoClient(connectionString);
        await client.DropDatabaseAsync(dbName);
    }

    private static async Task DropPostgresDatabaseAsync(int dataVolume)
    {
        var dbName = GetDatabaseName(BenchmarkProvider.PostgreSQL, dataVolume);
        var baseConnectionString = GetPostgresConnectionString();
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = "postgres" // Connect to default database to drop the benchmark database
        };

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        // Terminate all connections to the database before dropping
        await using var terminateCmd = new NpgsqlCommand(
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{dbName}' AND pid <> pg_backend_pid()",
            connection);
        await terminateCmd.ExecuteNonQueryAsync();

        // Drop the database
        await using var dropCmd = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{dbName}\"",
            connection);
        await dropCmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets the PostgreSQL connection string with the benchmark database name for a specific volume.
    /// Configured with appropriate pooling for high-concurrency benchmarks.
    /// </summary>
    private static string GetPostgresBenchmarkConnectionString(int dataVolume)
    {
        var dbName = GetDatabaseName(BenchmarkProvider.PostgreSQL, dataVolume);
        var baseConnectionString = GetPostgresConnectionString();
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = dbName,
            // Pooling configuration for high-concurrency benchmarks
            MaxPoolSize = 200,
            MinPoolSize = 10,
            ConnectionIdleLifetime = 300,
            Timeout = 30
        };
        return builder.ConnectionString;
    }

    /// <summary>
    /// Creates the benchmark database if it doesn't exist.
    /// </summary>
    private static async Task EnsurePostgresDatabaseExistsAsync(int dataVolume)
    {
        var dbName = GetDatabaseName(BenchmarkProvider.PostgreSQL, dataVolume);
        var baseConnectionString = GetPostgresConnectionString();
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = "postgres" // Connect to default database to create the benchmark database
        };

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        // Check if database exists
        await using var checkCmd = new NpgsqlCommand(
            $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'",
            connection);
        var exists = await checkCmd.ExecuteScalarAsync() != null;

        if (!exists)
        {
            await using var createCmd = new NpgsqlCommand(
                $"CREATE DATABASE \"{dbName}\"",
                connection);
            await createCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"  Created PostgreSQL database: {dbName}");
        }
    }
}
