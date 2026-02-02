namespace RecurringThings.Configuration;

using System;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder for configuring RecurringThings services.
/// </summary>
/// <remarks>
/// <para>
/// This builder is used to configure the persistence provider for RecurringThings.
/// Exactly one persistence provider (MongoDB or PostgreSQL) must be configured.
/// </para>
/// <para>
/// Use the extension methods in the provider-specific packages to configure the persistence:
/// <list type="bullet">
/// <item><c>UseMongoDb()</c> from RecurringThings.MongoDB</item>
/// <item><c>UsePostgreSql()</c> from RecurringThings.PostgreSQL</item>
/// </list>
/// </para>
/// </remarks>
public sealed class RecurringThingsBuilder
{
    /// <summary>
    /// Gets the service collection being configured.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets or sets a value indicating whether MongoDB has been configured.
    /// </summary>
    internal bool MongoDbConfigured { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether PostgreSQL has been configured.
    /// </summary>
    internal bool PostgreSqlConfigured { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringThingsBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    internal RecurringThingsBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    /// Validates that exactly one persistence provider has been configured.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when both providers are configured or no provider is configured.
    /// </exception>
    internal void Validate()
    {
        if (MongoDbConfigured && PostgreSqlConfigured)
        {
            throw new InvalidOperationException(
                "Cannot configure more than one persistence provider.");
        }

        if (!MongoDbConfigured && !PostgreSqlConfigured)
        {
            throw new InvalidOperationException(
                "Must configure a persistence provider. Use UseMongoDb() or UsePostgreSql().");
        }
    }
}
