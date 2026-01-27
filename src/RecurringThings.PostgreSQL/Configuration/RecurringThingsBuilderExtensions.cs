namespace RecurringThings.PostgreSQL.Configuration;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecurringThings.Configuration;
using RecurringThings.PostgreSQL.Data;
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

        // Register EF Core DbContext factory
        builder.Services.AddDbContextFactory<RecurringThingsDbContext>(dbOptions =>
            dbOptions.UseNpgsql(options.ConnectionString));

        // Run migrations if configured
        if (options.RunMigrationsOnStartup)
        {
            // Use advisory lock for concurrent migration safety
            var dbOptions = new DbContextOptionsBuilder<RecurringThingsDbContext>()
                .UseNpgsql(options.ConnectionString)
                .Options;
            using var context = new RecurringThingsDbContext(dbOptions);
            var migrationService = new AdvisoryLockMigrationService(context);
            migrationService.Migrate();
        }

        // Register repositories with DbContext factory
        builder.Services.AddScoped<IRecurrenceRepository>(sp =>
            new PostgresRecurrenceRepository(
                sp.GetRequiredService<IDbContextFactory<RecurringThingsDbContext>>(),
                options.ConnectionString));

        builder.Services.AddScoped<IOccurrenceRepository>(sp =>
            new PostgresOccurrenceRepository(
                sp.GetRequiredService<IDbContextFactory<RecurringThingsDbContext>>(),
                options.ConnectionString));

        builder.Services.AddScoped<IOccurrenceExceptionRepository>(sp =>
            new PostgresOccurrenceExceptionRepository(
                sp.GetRequiredService<IDbContextFactory<RecurringThingsDbContext>>(),
                options.ConnectionString));

        builder.Services.AddScoped<IOccurrenceOverrideRepository>(sp =>
            new PostgresOccurrenceOverrideRepository(
                sp.GetRequiredService<IDbContextFactory<RecurringThingsDbContext>>(),
                options.ConnectionString));

        return builder;
    }
}
