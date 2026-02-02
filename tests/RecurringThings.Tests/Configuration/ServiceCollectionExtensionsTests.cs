namespace RecurringThings.Tests.Configuration;

using Microsoft.Extensions.DependencyInjection;
using RecurringThings.Configuration;
using RecurringThings.Engine;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Tests that AddRecurringThings throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void GivenNullServices_WhenAddRecurringThings_ThenThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => services.AddRecurringThings(_ => { }));
        Assert.Equal("services", exception.ParamName);
    }

    /// <summary>
    /// Tests that AddRecurringThings throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void GivenNullConfigure_WhenAddRecurringThings_ThenThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => services.AddRecurringThings(null!));
        Assert.Equal("configure", exception.ParamName);
    }

    /// <summary>
    /// Tests that AddRecurringThings throws InvalidOperationException when no provider is configured.
    /// </summary>
    [Fact]
    public void GivenNoProviderConfigured_WhenAddRecurringThings_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddRecurringThings(_ => { }));
        Assert.Contains("Must configure a persistence provider", exception.Message);
    }

    /// <summary>
    /// Tests that AddRecurringThings throws InvalidOperationException when both providers are configured.
    /// </summary>
    [Fact]
    public void GivenBothProvidersConfigured_WhenAddRecurringThings_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddRecurringThings(builder =>
            {
                builder.MongoDbConfigured = true;
                builder.PostgreSqlConfigured = true;
            }));
        Assert.Contains("Cannot configure more than one persistence provider", exception.Message);
    }

    /// <summary>
    /// Tests that AddRecurringThings registers IRecurrenceEngine when MongoDB is configured.
    /// </summary>
    [Fact]
    public void GivenMongoDbConfigured_WhenAddRecurringThings_ThenRegistersRecurrenceEngine()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRecurringThings(builder => builder.MongoDbConfigured = true);

        // Assert
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IRecurrenceEngine));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(RecurrenceEngine), descriptor.ImplementationType);
    }

    /// <summary>
    /// Tests that AddRecurringThings registers IRecurrenceEngine when PostgreSQL is configured.
    /// </summary>
    [Fact]
    public void GivenPostgreSqlConfigured_WhenAddRecurringThings_ThenRegistersRecurrenceEngine()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRecurringThings(builder => builder.PostgreSqlConfigured = true);

        // Assert
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IRecurrenceEngine));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(RecurrenceEngine), descriptor.ImplementationType);
    }

    /// <summary>
    /// Tests that AddRecurringThings returns the same service collection for chaining.
    /// </summary>
    [Fact]
    public void GivenValidConfiguration_WhenAddRecurringThings_ThenReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRecurringThings(builder => builder.MongoDbConfigured = true);

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    /// Tests that AddRecurringThings passes the builder to the configure action.
    /// </summary>
    [Fact]
    public void GivenConfigureAction_WhenAddRecurringThings_ThenPassesBuilderWithServices()
    {
        // Arrange
        var services = new ServiceCollection();
        RecurringThingsBuilder? capturedBuilder = null;

        // Act
        services.AddRecurringThings(builder =>
        {
            capturedBuilder = builder;
            builder.MongoDbConfigured = true;
        });

        // Assert
        Assert.NotNull(capturedBuilder);
        Assert.Same(services, capturedBuilder.Services);
    }
}
