namespace RecurringThings.Configuration;

using System;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RecurringThings.Engine;
using RecurringThings.Validation.Validators;

/// <summary>
/// Extension methods for configuring RecurringThings services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds RecurringThings services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">A delegate to configure the RecurringThings builder.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when both providers are configured or no provider is configured.</exception>
    /// <remarks>
    /// <para>
    /// This method must be used with a persistence provider configuration.
    /// Use one of the following extension methods from the provider-specific packages:
    /// </para>
    /// <code>
    /// // Using MongoDB
    /// services.AddRecurringThings(builder =>
    ///     builder.UseMongoDb(options =>
    ///     {
    ///         options.ConnectionString = "mongodb://localhost:27017";
    ///         options.DatabaseName = "myapp";
    ///     }));
    ///
    /// // Using PostgreSQL
    /// services.AddRecurringThings(builder =>
    ///     builder.UsePostgreSql(options =>
    ///     {
    ///         options.ConnectionString = "Host=localhost;Database=myapp";
    ///     }));
    /// </code>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddRecurringThings(builder =>
    ///     builder.UseMongoDb(options =>
    ///     {
    ///         options.ConnectionString = "mongodb://localhost:27017";
    ///         options.DatabaseName = "recurring_things";
    ///     }));
    /// </code>
    /// </example>
    public static IServiceCollection AddRecurringThings(
        this IServiceCollection services,
        Action<RecurringThingsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new RecurringThingsBuilder(services);
        configure(builder);

        // Validate that exactly one provider has been configured
        builder.Validate();

        // Register FluentValidation validators from the assembly (including internal validators)
        services.AddValidatorsFromAssemblyContaining<RecurrenceCreateValidator>(
            ServiceLifetime.Scoped,
            includeInternalTypes: true);

        // Register the recurrence engine as scoped
        // The repositories are registered by the provider-specific UseMongoDb/UsePostgreSql methods
        services.AddScoped<IRecurrenceEngine, RecurrenceEngine>();

        return services;
    }
}
