namespace RecurringThings.PostgreSQL.Tests.Integration;

using RecurringThings.Tests.Integration;

/// <summary>
/// PostgreSQL integration tests that inherit shared test cases from IntegrationTestsBase.
/// </summary>
/// <remarks>
/// All test methods are defined in <see cref="IntegrationTestsBase"/> and run against
/// the PostgreSQL provider via the <see cref="PostgreSqlFixture"/>.
/// </remarks>
public sealed class PostgreSqlIntegrationTests : IntegrationTestsBase, IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    protected override IServiceProvider? Provider => _fixture.Provider;

    /// <inheritdoc />
    protected override string EnvironmentVariableName => "POSTGRES_CONNECTION_STRING";
}
