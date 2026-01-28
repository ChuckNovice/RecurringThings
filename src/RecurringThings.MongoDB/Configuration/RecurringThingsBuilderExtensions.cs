namespace RecurringThings.MongoDB.Configuration;

using System;
using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using RecurringThings.Configuration;
using RecurringThings.MongoDB.Indexing;
using RecurringThings.MongoDB.Repositories;
using RecurringThings.Repository;

/// <summary>
/// Extension methods for configuring MongoDB persistence with RecurringThings.
/// </summary>
public static class RecurringThingsBuilderExtensions
{
    /// <summary>
    /// Configures RecurringThings to use MongoDB for persistence.
    /// </summary>
    /// <param name="builder">The RecurringThings builder.</param>
    /// <param name="configure">A delegate to configure MongoDB options.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required MongoDB options are missing.</exception>
    /// <example>
    /// <code>
    /// services.AddRecurringThings(builder =>
    ///     builder.UseMongoDb(options =>
    ///     {
    ///         options.ConnectionString = "mongodb://localhost:27017";
    ///         options.DatabaseName = "myapp";
    ///         options.CollectionName = "recurring_things"; // Optional, defaults to "recurring_things"
    ///     }));
    /// </code>
    /// </example>
    public static RecurringThingsBuilder UseMongoDb(
        this RecurringThingsBuilder builder,
        Action<MongoDbOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MongoDbOptions();
        configure(options);
        options.Validate();

        // Ensure MongoDB conventions are registered (idempotent)
        MongoDbInitializer.EnsureInitialized();

        // Mark MongoDB as configured
        builder.MongoDbConfigured = true;

        // Register MongoDB client and database
        builder.Services.AddSingleton<IMongoClient>(_ =>
            new MongoClient(options.ConnectionString));

        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName));

        // Register repositories
        builder.Services.AddScoped<IRecurrenceRepository>(sp =>
            new MongoRecurrenceRepository(
                sp.GetRequiredService<IMongoDatabase>(),
                options.CollectionName));

        builder.Services.AddScoped<IOccurrenceRepository>(sp =>
            new MongoOccurrenceRepository(
                sp.GetRequiredService<IMongoDatabase>(),
                options.CollectionName));

        builder.Services.AddScoped<IOccurrenceExceptionRepository>(sp =>
            new MongoOccurrenceExceptionRepository(
                sp.GetRequiredService<IMongoDatabase>(),
                options.CollectionName));

        builder.Services.AddScoped<IOccurrenceOverrideRepository>(sp =>
            new MongoOccurrenceOverrideRepository(
                sp.GetRequiredService<IMongoDatabase>(),
                options.CollectionName));

        // Register IndexManager as singleton
        builder.Services.AddSingleton(sp =>
            new IndexManager(
                sp.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName),
                options.CollectionName));

        // Create indexes on startup if configured
        if (options.CreateIndexesOnStartup)
        {
            var client = new MongoClient(options.ConnectionString);
            var database = client.GetDatabase(options.DatabaseName);
            var indexManager = new IndexManager(database, options.CollectionName);
            indexManager.EnsureIndexesAsync().GetAwaiter().GetResult();
        }

        return builder;
    }
}
