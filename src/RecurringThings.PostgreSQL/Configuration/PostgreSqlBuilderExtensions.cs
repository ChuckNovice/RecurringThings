namespace RecurringThings.PostgreSQL.Configuration;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurringThings.Configuration;
using RecurringThings.PostgreSQL.Data;
using RecurringThings.PostgreSQL.Repositories;
using RecurringThings.Repository;

/// <summary>
/// Extension methods for configuring PostgreSQL persistence with RecurringThings.
/// </summary>
public static class PostgreSqlBuilderExtensions
{
    /// <summary>
    /// Configures RecurringThings to use PostgreSQL for persistence.
    /// </summary>
    /// <param name="builder">The RecurringThings builder.</param>
    /// <param name="configure">A delegate to configure PostgreSQL options with access to the service provider.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <example>
    /// Basic configuration:
    /// <code>
    /// services.AddRecurringThings(builder =>
    ///     builder.UsePostgreSql((provider, options) =>
    ///     {
    ///         options.ConnectionString = "Host=localhost;Database=myapp;Username=user;Password=pass";
    ///     }));
    /// </code>
    /// 
    /// Advanced configuration with DbContext customization:
    /// <code>
    /// services.AddRecurringThings(builder =>
    ///     builder.UsePostgreSql((provider, options) =>
    ///     {
    ///         var config = provider.GetRequiredService&lt;IConfiguration&gt;();
    ///         options.ConnectionString = config.GetConnectionString("RecurringThings");
    ///         
    ///         options.ConfigureNpgsql = npgsqlOptions =>
    ///         {
    ///             // Enable retry on failure
    ///             npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
    ///             npgsqlOptions.CommandTimeout(30);
    ///             npgsqlOptions.MigrationsHistoryTable("__my_migrations");
    ///         };
    ///     }));
    /// </code>
    /// </example>
    public static RecurringThingsBuilder UsePostgreSql(
        this RecurringThingsBuilder builder,
        Action<IServiceProvider, PostgreSqlOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        // Register options with IServiceProvider access
        builder.Services.AddOptions<PostgreSqlOptions>()
            .Configure<IServiceProvider>((options, sp) =>
            {
                configure(sp, options);
                options.Validate();
            });

        // Mark PostgreSQL as configured
        builder.PostgreSqlConfigured = true;
        builder.Services.AddPostgresTransactionManager();

        // Register advisory lock service as singleton
        builder.Services.AddSingleton<AdvisoryLockService>();

        // Register migration initializer as singleton
        builder.Services.AddSingleton<PostgresMigrationInitializer>();

        // Register EF Core DbContext factory with runtime options resolution
        // Must be registered before AdvisoryLockService which depends on it
        builder.Services.AddDbContextFactory<RecurringThingsDbContext>((provider, dbOptions) =>
        {
            var options = provider.GetRequiredService<IOptions<PostgreSqlOptions>>().Value;

            dbOptions.UseNpgsql(options.ConnectionString, npgsqlOptions =>
                // Allow user to customize Npgsql options
                options.ConfigureNpgsql?.Invoke(npgsqlOptions));
        });

        // Register the repository.
        builder.Services.AddRepository<IRecurringThingsRepository, PostgresRecurringThingsRepository>();

        return builder;
    }

    /// <summary>
    /// Registers a repository as a singleton with automatic migration initialization.
    /// </summary>
    /// <typeparam name="TService">The repository interface type.</typeparam>
    /// <typeparam name="TImplementation">The repository implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method ensures database migrations are executed before the first repository instance is created.
    /// The <see cref="PostgresMigrationInitializer.EnsureMigrated"/> call uses internal locking to guarantee
    /// migrations run only once, even when multiple repositories are resolved concurrently.
    /// </para>
    /// </remarks>
    private static IServiceCollection AddRepository<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        return services.AddSingleton<TService, TImplementation>(provider =>
        {
            // Ensure migrations have run before creating the first repository instance
            var options = provider.GetRequiredService<IOptions<PostgreSqlOptions>>().Value;
            provider.GetRequiredService<PostgresMigrationInitializer>().EnsureMigrated();

            return ActivatorUtilities.CreateInstance<TImplementation>(provider);
        });
    }
}
