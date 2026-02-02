namespace RecurringThings.MongoDB.Configuration;

using System;
using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurringThings.Configuration;
using RecurringThings.MongoDB.Indexing;
using RecurringThings.MongoDB.Repositories;
using RecurringThings.Repository;

/// <summary>
/// Extension methods for configuring MongoDB persistence with RecurringThings.
/// </summary>
public static class MongoDbBuilderExtensions
{
    /// <summary>
    /// Configures RecurringThings to use MongoDB for persistence.
    /// </summary>
    /// <param name="builder">The RecurringThings builder.</param>
    /// <param name="configure">A delegate to configure MongoDB options.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <example>
    /// Basic configuration:
    /// <code>
    /// services.AddRecurringThings(builder =>
    ///     builder.UseMongoDb((provider, options) =>
    ///     {
    ///         options.ConnectionString = "mongodb://localhost:27017";
    ///         options.DatabaseName = "myapp";
    ///         options.CollectionName = "mycalendar";
    ///     }));
    /// </code>
    /// 
    /// Advanced configuration with MongoClientSettings customization:
    /// <code>
    /// services.AddRecurringThings(builder =>
    ///     builder.UseMongoDb((provider, options) =>
    ///     {
    ///         var config = provider.GetRequiredService&lt;IConfiguration&gt;();
    ///         options.ConnectionString = config.GetConnectionString("RecurringThings");
    ///         options.DatabaseName = "myapp";
    ///         options.CollectionName = "mycalendar";
    ///         
    ///         options.ConfigureClientSettings = settings =>
    ///         {
    ///             // Connection pooling
    ///             settings.MaxConnectionPoolSize = 500;
    ///             settings.MinConnectionPoolSize = 10;
    ///             
    ///             // Timeouts
    ///             settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
    ///             settings.SocketTimeout = TimeSpan.FromSeconds(30);
    ///         };
    ///     }));
    /// </code>
    /// </example>
    public static RecurringThingsBuilder UseMongoDb(
        this RecurringThingsBuilder builder,
        Action<IServiceProvider, MongoDbOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        // Register options configuration that will be resolved at runtime
        builder.Services.AddOptions<MongoDbOptions>()
            .Configure<IServiceProvider>((options, sp) =>
            {
                configure(sp, options);
                options.Validate();
            });

        // Ensure MongoDB conventions are registered (idempotent)
        MongoDbInitializer.EnsureInitialized();

        // Mark MongoDB as configured
        builder.MongoDbConfigured = true;
        builder.Services.AddMongoDbTransactionManager();

        // Register IndexManager as singleton
        builder.Services.AddSingleton<IndexManager>();

        // Register MongoDB client
        builder.Services.AddKeyedSingleton<IMongoClient>("RecurringThings_MongoClient", (provider, key) =>
        {
            var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);

            // Allow user to customize settings
            options.ConfigureClientSettings?.Invoke(settings);

            return new MongoClient(settings);
        });

        // Register MongoDB database
        builder.Services.AddKeyedSingleton("RecurringThings_MongoDatabase", (provider, key) =>
        {
            var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var client = provider.GetRequiredKeyedService<IMongoClient>("RecurringThings_MongoClient");
            var database = client.GetDatabase(options.DatabaseName);
            return database;
        });

        // Register repository
        builder.Services.AddRepository<IRecurringThingsRepository, MongoRecurringThingsRepository>();

        return builder;
    }

    /// <summary>
    /// Registers a repository as a singleton with automatic indexes initialization.
    /// </summary>
    /// <typeparam name="TService">The repository interface type.</typeparam>
    /// <typeparam name="TImplementation">The repository implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method ensures indexes are created before the first repository instance is created.
    /// The <see cref="IndexManager.EnsureIndexes"/> call uses internal locking to guarantee
    /// indexes are attempted to be created only once, even when multiple repositories are resolved concurrently.
    /// </para>
    /// </remarks>
    private static IServiceCollection AddRepository<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        return services.AddSingleton<TService, TImplementation>(provider =>
        {
            // Ensure migrations have run before creating the first repository instance
            var indexManager = provider.GetRequiredService<IndexManager>();
            indexManager.EnsureIndexes();

            return ActivatorUtilities.CreateInstance<TImplementation>(provider);
        });
    }
}
