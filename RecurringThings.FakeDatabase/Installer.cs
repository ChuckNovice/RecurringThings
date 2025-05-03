namespace RecurringThings.FakeDatabase;

using Microsoft.Extensions.DependencyInjection;
using RecurringThings.Core.Repository;
using Repository;

/// <summary>
///     Provides extension methods for registering repository services.
/// </summary>
public static class Installer
{

    /// <summary>
    ///     Registers data services and repository implementations with the specified service collection.
    /// </summary>
    /// <param name="services">
    ///     The service collection to register services into.
    /// </param>
    /// <returns>
    ///     The updated <see cref="IServiceCollection"/>.
    /// </returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services
            .AddScoped<IRecurrenceRepository, RecurrenceRepository>()
            .AddScoped<IOccurrenceRepository, OccurrenceRepository>();

        return services;
    }
}
