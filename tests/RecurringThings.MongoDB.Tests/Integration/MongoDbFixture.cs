namespace RecurringThings.MongoDB.Tests.Integration;

using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using RecurringThings.Configuration;
using RecurringThings.Engine;
using RecurringThings.MongoDB.Configuration;

/// <summary>
/// Fixture that provides a MongoDB-backed IServiceProvider for integration tests.
/// </summary>
/// <remarks>
/// <para>
/// The Provider property is null if MONGODB_CONNECTION_STRING is not set.
/// Tests should check for null and skip accordingly.
/// </para>
/// <para>
/// Creates a unique database per test run and drops it on disposal.
/// </para>
/// </remarks>
public sealed class MongoDbFixture : IAsyncDisposable
{
    private const string CollectionName = "recurring_things";

    private readonly string? _connectionString;
    private readonly string _databaseName;
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the service provider, or null if MONGODB_CONNECTION_STRING is not set.
    /// </summary>
    public IServiceProvider? Provider => _serviceProvider;

    public MongoDbFixture()
    {
        _connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
        // MongoDB database name limit is 63 characters
        // Format: rt_test_<guid> to ensure uniqueness for parallel execution
        _databaseName = $"rt_test_{Guid.NewGuid():N}"[..40];

        if (!string.IsNullOrEmpty(_connectionString))
        {
            InitializeServices();
        }
    }

    private void InitializeServices()
    {
        var services = new ServiceCollection();
        services.AddRecurringThings(builder =>
            builder.UseMongoDb((provider, options) =>
            {
                options.ConnectionString = _connectionString!;
                options.DatabaseName = _databaseName;
                options.CollectionName = CollectionName;
                options.CreateIndexesOnStartup = true;
            }));

        _serviceProvider = services.BuildServiceProvider();

        // Trigger index creation by resolving the engine
        _ = _serviceProvider.GetRequiredService<IRecurrenceEngine>();
    }

    public async ValueTask DisposeAsync()
    {
        // Dispose service provider first to release connections
        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync();
        }

        // Drop the test database using a separate client
        if (!string.IsNullOrEmpty(_connectionString))
        {
            var cleanupClient = new MongoClient(_connectionString);
            await cleanupClient.DropDatabaseAsync(_databaseName);
        }
    }
}
