namespace RecurringThings.Tests.Configuration;

using Microsoft.Extensions.DependencyInjection;
using RecurringThings.Configuration;

/// <summary>
/// Unit tests for <see cref="RecurringThingsBuilder"/>.
/// </summary>
public sealed class RecurringThingsBuilderTests
{
    /// <summary>
    /// Tests that constructor throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void GivenNullServices_WhenConstructed_ThenThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        // Note: Constructor is internal, so we test via AddRecurringThings which creates the builder
        // The builder constructor throws, but it's wrapped by the extension method's null check
        // which happens first. This test verifies the behavior through the public API.
        IServiceCollection services = null!;

        var exception = Assert.Throws<ArgumentNullException>(
            () => services.AddRecurringThings(_ => { }));
        Assert.Equal("services", exception.ParamName);
    }

    /// <summary>
    /// Tests that Validate throws InvalidOperationException when no provider is configured.
    /// </summary>
    [Fact]
    public void GivenNoProviderConfigured_WhenValidate_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Validate());
        Assert.Contains("Must configure a persistence provider", exception.Message);
        Assert.Contains("UseMongoDb()", exception.Message);
        Assert.Contains("UsePostgreSql()", exception.Message);
    }

    /// <summary>
    /// Tests that Validate throws InvalidOperationException when both providers are configured.
    /// </summary>
    [Fact]
    public void GivenBothProvidersConfigured_WhenValidate_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);
        builder.MongoDbConfigured = true;
        builder.PostgreSqlConfigured = true;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Validate());
        Assert.Contains("Cannot configure more than one persistence provider", exception.Message);
    }

    /// <summary>
    /// Tests that Validate succeeds when only MongoDB is configured.
    /// </summary>
    [Fact]
    public void GivenOnlyMongoDbConfigured_WhenValidate_ThenDoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);
        builder.MongoDbConfigured = true;

        // Act & Assert
        var exception = Record.Exception(() => builder.Validate());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Validate succeeds when only PostgreSQL is configured.
    /// </summary>
    [Fact]
    public void GivenOnlyPostgreSqlConfigured_WhenValidate_ThenDoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);
        builder.PostgreSqlConfigured = true;

        // Act & Assert
        var exception = Record.Exception(() => builder.Validate());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that MongoDbConfigured defaults to false.
    /// </summary>
    [Fact]
    public void GivenNewBuilder_WhenCheckMongoDbConfigured_ThenReturnsFalse()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        // Act & Assert
        Assert.False(builder.MongoDbConfigured);
    }

    /// <summary>
    /// Tests that PostgreSqlConfigured defaults to false.
    /// </summary>
    [Fact]
    public void GivenNewBuilder_WhenCheckPostgreSqlConfigured_ThenReturnsFalse()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateBuilder(services);

        // Act & Assert
        Assert.False(builder.PostgreSqlConfigured);
    }

    /// <summary>
    /// Tests that Services property returns the service collection passed to constructor.
    /// </summary>
    [Fact]
    public void GivenServicesPassedToConstructor_WhenAccessServicesProperty_ThenReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = CreateBuilder(services);

        // Assert
        Assert.Same(services, builder.Services);
    }

    /// <summary>
    /// Helper method to create a RecurringThingsBuilder via reflection since constructor is internal.
    /// </summary>
    private static RecurringThingsBuilder CreateBuilder(IServiceCollection services)
    {
        // The constructor is internal but accessible due to InternalsVisibleTo
        return new RecurringThingsBuilder(services);
    }
}
