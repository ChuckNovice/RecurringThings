namespace RecurringThings.PostgreSQL.Configuration;

using System;
using Microsoft.Extensions.DependencyInjection;
using RecurringThings.Configuration;
using RecurringThings.PostgreSQL.Migrations;
using RecurringThings.PostgreSQL.Repositories;
using RecurringThings.Repository;

/// <summary>
/// Extension methods for configuring PostgreSQL persistence with RecurringThings.
/// </summary>
public static class RecurringThingsBuilderExtensions
{
    /// <summary>
    /// Configures RecurringThings to use PostgreSQL for persistence.
    /// </summary>
    /// <param name="builder">The RecurringThings builder.</param>
    /// <param name="configure">A delegate to configure PostgreSQL options.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required PostgreSQL options are missing.</exception>
    /// <example>
    /// <code>
    /// services.AddRecurringThings(builder =>
    ///     builder.UsePostgreSql(options =>
    ///     {
    ///         options.ConnectionString = "Host=localhost;Database=myapp;Username=user;Password=pass";
    ///         options.RunMigrationsOnStartup = true; // Optional, defaults to true
    ///     }));
    /// </code>
    /// </example>
    public static RecurringThingsBuilder UsePostgreSql(
        this RecurringThingsBuilder builder,
        Action<PostgreSqlOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new PostgreSqlOptions();
        configure(options);
        options.Validate();

        // Mark PostgreSQL as configured
        builder.PostgreSqlConfigured = true;

        // Run migrations if configured
        if (options.RunMigrationsOnStartup)
        {
            var migrationEngine = new MigrationEngine(options.ConnectionString);
            migrationEngine.EnsureDatabaseMigratedAsync().GetAwaiter().GetResult();
        }

        // Register repositories
        builder.Services.AddScoped<IRecurrenceRepository>(_ =>
            new PostgresRecurrenceRepository(options.ConnectionString));

        builder.Services.AddScoped<IOccurrenceRepository>(_ =>
            new PostgresOccurrenceRepository(options.ConnectionString));

        builder.Services.AddScoped<IOccurrenceExceptionRepository>(_ =>
            new PostgresOccurrenceExceptionRepository(options.ConnectionString));

        builder.Services.AddScoped<IOccurrenceOverrideRepository>(_ =>
            new PostgresOccurrenceOverrideRepository(options.ConnectionString));

        return builder;
    }
}
