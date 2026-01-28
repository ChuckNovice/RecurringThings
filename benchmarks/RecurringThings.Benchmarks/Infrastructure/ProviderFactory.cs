namespace RecurringThings.Benchmarks.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
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
/// </summary>
public static class ProviderFactory
{
    // Static database names to ensure same DB is used across benchmark runs
    private static readonly string MongoDatabaseName = $"rt_bench_{DateTime.UtcNow:yyyyMMdd}";

    private static bool _mongoInitialized;
    private static bool _postgresInitialized;

    /// <summary>
    /// Initializes the provider by running migrations/creating indexes.
    /// Called once at startup before any benchmarks run.
    /// </summary>
    public static async Task InitializeProviderAsync(BenchmarkProvider provider)
    {
        Console.WriteLine($"Initializing {provider} provider (migrations/indexes)...");

        switch (provider)
        {
            case BenchmarkProvider.MongoDB:
                if (!_mongoInitialized)
                {
                    // MongoDB index creation happens automatically on first use via IndexManager
                    // Force it now by creating a temporary engine and making a query
                    var engine = CreateEngine(provider);
                    // Trigger index creation by making a simple query
                    await foreach (var _ in engine.GetRecurrencesAsync(
                        "init", "init", DateTime.UtcNow, DateTime.UtcNow.AddDays(1)))
                    {
                        break;
                    }

                    _mongoInitialized = true;
                    Console.WriteLine($"  MongoDB database: {MongoDatabaseName}");
                }

                break;

            case BenchmarkProvider.PostgreSQL:
                if (!_postgresInitialized)
                {
                    // PostgreSQL migrations run via EF Core on first DbContext use
                    var engine = CreateEngine(provider);
                    // Trigger migrations by making a simple query
                    await foreach (var _ in engine.GetRecurrencesAsync(
                        "init", "init", DateTime.UtcNow, DateTime.UtcNow.AddDays(1)))
                    {
                        break;
                    }

                    _postgresInitialized = true;
                    Console.WriteLine("  PostgreSQL database ready");
                }

                break;
        }

        Console.WriteLine($"{provider} initialization complete");
    }

    /// <summary>
    /// Creates a new IRecurrenceEngine instance for the specified provider.
    /// </summary>
    public static IRecurrenceEngine CreateEngine(BenchmarkProvider provider)
    {
        var services = new ServiceCollection();

        services.AddRecurringThings(builder =>
        {
            switch (provider)
            {
                case BenchmarkProvider.MongoDB:
                    builder.UseMongoDb(options =>
                    {
                        options.ConnectionString = GetMongoConnectionString();
                        options.DatabaseName = MongoDatabaseName;
                        options.CollectionName = "recurring_things";
                    });
                    break;

                case BenchmarkProvider.PostgreSQL:
                    builder.UsePostgreSql(options =>
                    {
                        options.ConnectionString = GetPostgresConnectionString();
                    });
                    break;
            }
        });

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IRecurrenceEngine>();
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
}
