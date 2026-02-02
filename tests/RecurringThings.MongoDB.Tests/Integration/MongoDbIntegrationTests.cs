namespace RecurringThings.MongoDB.Tests.Integration;

using RecurringThings.Tests.Integration;

/// <summary>
/// MongoDB integration tests that inherit shared test cases from IntegrationTestsBase.
/// </summary>
/// <remarks>
/// All test methods are defined in <see cref="IntegrationTestsBase"/> and run against
/// the MongoDB provider via the <see cref="MongoDbFixture"/>.
/// </remarks>
public sealed class MongoDbIntegrationTests : IntegrationTestsBase, IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;

    public MongoDbIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    protected override IServiceProvider? Provider => _fixture.Provider;

    /// <inheritdoc />
    protected override string EnvironmentVariableName => "MONGODB_CONNECTION_STRING";
}
