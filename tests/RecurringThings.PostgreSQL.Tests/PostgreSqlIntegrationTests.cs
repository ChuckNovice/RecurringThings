namespace RecurringThings.PostgreSQL.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RecurringThings.Domain;
using RecurringThings.PostgreSQL.Data;
using RecurringThings.PostgreSQL.Repositories;
using Xunit;

/// <summary>
/// Integration tests for PostgreSQL repositories.
/// </summary>
/// <remarks>
/// Requires POSTGRES_CONNECTION_STRING environment variable to be set.
/// Tests are skipped if the environment variable is not available.
/// The connection string should point to a PostgreSQL server where the test can create/drop databases.
/// </remarks>
[Trait("Category", "Integration")]
public class PostgreSqlIntegrationTests : IAsyncLifetime
{
    private readonly string? _baseConnectionString;
    private readonly string _testDatabaseName;
    private string? _testConnectionString;
    private IDbContextFactory<RecurringThingsDbContext>? _contextFactory;

    private const string TestOrganization = "test-org";
    private const string TestResourcePath = "test/path";
    private const string TestType = "appointment";
    private const string TestTimeZone = "America/New_York";

    public PostgreSqlIntegrationTests()
    {
        _baseConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        _testDatabaseName = $"recurring_things_test_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}"[..63]; // PostgreSQL limit is 63 chars
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(_baseConnectionString))
        {
            return;
        }

        // Create the test database
        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync();

        await using var createDbCommand = new NpgsqlCommand(
            $"CREATE DATABASE \"{_testDatabaseName}\"", connection);
        await createDbCommand.ExecuteNonQueryAsync();

        // Build connection string for the test database
        var builder = new NpgsqlConnectionStringBuilder(_baseConnectionString)
        {
            Database = _testDatabaseName
        };
        _testConnectionString = builder.ConnectionString;

        // Create DbContext factory for tests
        var services = new ServiceCollection();
        services.AddDbContextFactory<RecurringThingsDbContext>(options =>
            options.UseNpgsql(_testConnectionString));

        var serviceProvider = services.BuildServiceProvider();
        _contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<RecurringThingsDbContext>>();

        // Run EF Core migrations with advisory lock
        var dbOptions = new DbContextOptionsBuilder<RecurringThingsDbContext>()
            .UseNpgsql(_testConnectionString)
            .Options;
        await using var context = new RecurringThingsDbContext(dbOptions);
        var migrationService = new AdvisoryLockMigrationService(context);
        await migrationService.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(_baseConnectionString))
        {
            return;
        }

        // Drop the test database
        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync();

        // Terminate connections to the test database
        await using var terminateCommand = new NpgsqlCommand($"""
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = '{_testDatabaseName}'
            AND pid <> pg_backend_pid()
            """, connection);
        await terminateCommand.ExecuteNonQueryAsync();

        // Drop the database
        await using var dropDbCommand = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{_testDatabaseName}\"", connection);
        await dropDbCommand.ExecuteNonQueryAsync();
    }

    private void SkipIfNoConnection()
    {
        Skip.If(string.IsNullOrEmpty(_baseConnectionString),
            "POSTGRES_CONNECTION_STRING environment variable not set");
    }

    private PostgresRecurrenceRepository CreateRecurrenceRepository()
    {
        return new PostgresRecurrenceRepository(_contextFactory!, _testConnectionString!);
    }

    private PostgresOccurrenceRepository CreateOccurrenceRepository()
    {
        return new PostgresOccurrenceRepository(_contextFactory!, _testConnectionString!);
    }

    private PostgresOccurrenceExceptionRepository CreateExceptionRepository()
    {
        return new PostgresOccurrenceExceptionRepository(_contextFactory!, _testConnectionString!);
    }

    private PostgresOccurrenceOverrideRepository CreateOverrideRepository()
    {
        return new PostgresOccurrenceOverrideRepository(_contextFactory!, _testConnectionString!);
    }

    #region Recurrence Repository Tests

    [SkippableFact]
    public async Task RecurrenceRepository_CreateAndGetById_ReturnsCreatedRecurrence()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateRecurrenceRepository();
        var recurrence = CreateRecurrence();

        // Act
        await repo.CreateAsync(recurrence);
        var result = await repo.GetByIdAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(recurrence.Id, result!.Id);
        Assert.Equal(TestOrganization, result.Organization);
        Assert.Equal(TestResourcePath, result.ResourcePath);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(recurrence.Duration, result.Duration);
        Assert.Equivalent(recurrence.Extensions, result.Extensions);
    }

    [SkippableFact]
    public async Task RecurrenceRepository_Update_UpdatesDurationAndExtensions()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateRecurrenceRepository();
        var recurrence = CreateRecurrence();
        await repo.CreateAsync(recurrence);

        // Act
        recurrence.Duration = TimeSpan.FromHours(2);
        recurrence.Extensions = new Dictionary<string, string> { ["updated"] = "true" };
        await repo.UpdateAsync(recurrence);

        var result = await repo.GetByIdAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromHours(2), result!.Duration);
        Assert.True(result.Extensions?.ContainsKey("updated") == true);
    }

    [SkippableFact]
    public async Task RecurrenceRepository_Delete_RemovesRecurrence()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateRecurrenceRepository();
        var recurrence = CreateRecurrence();
        await repo.CreateAsync(recurrence);

        // Act
        await repo.DeleteAsync(recurrence.Id, TestOrganization, TestResourcePath);
        var result = await repo.GetByIdAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.Null(result);
    }

    [SkippableFact]
    public async Task RecurrenceRepository_GetInRange_ReturnsRecurrencesInRange()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateRecurrenceRepository();
        var recurrence = CreateRecurrence();
        await repo.CreateAsync(recurrence);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await repo.GetInRangeAsync(
            TestOrganization, TestResourcePath, queryStart, queryEnd, null).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(recurrence.Id, results[0].Id);
    }

    [SkippableFact]
    public async Task RecurrenceRepository_GetInRange_FiltersOutOfRange()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateRecurrenceRepository();
        var recurrence = CreateRecurrence();
        await repo.CreateAsync(recurrence);

        // Query range before recurrence
        var queryStart = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2020, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await repo.GetInRangeAsync(
            TestOrganization, TestResourcePath, queryStart, queryEnd, null).ToListAsync();

        // Assert
        Assert.Empty(results);
    }

    [SkippableFact]
    public async Task RecurrenceRepository_GetInRange_FiltersByType()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateRecurrenceRepository();
        var recurrence1 = CreateRecurrence();
        var recurrence2 = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = "meeting",
            StartTime = recurrence1.StartTime,
            Duration = recurrence1.Duration,
            RecurrenceEndTime = recurrence1.RecurrenceEndTime,
            RRule = recurrence1.RRule,
            TimeZone = recurrence1.TimeZone,
            Extensions = recurrence1.Extensions
        };

        await repo.CreateAsync(recurrence1);
        await repo.CreateAsync(recurrence2);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await repo.GetInRangeAsync(
            TestOrganization, TestResourcePath, queryStart, queryEnd, [TestType]).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(TestType, results[0].Type);
    }

    #endregion

    #region Occurrence Repository Tests

    [SkippableFact]
    public async Task OccurrenceRepository_CreateAndGetById_ReturnsCreatedOccurrence()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateOccurrenceRepository();
        var occurrence = CreateOccurrence();

        // Act
        await repo.CreateAsync(occurrence);
        var result = await repo.GetByIdAsync(occurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(occurrence.Id, result!.Id);
        Assert.Equal(occurrence.StartTime, result.StartTime);
        Assert.Equal(occurrence.EndTime, result.EndTime);
        Assert.Equal(occurrence.Duration, result.Duration);
    }

    [SkippableFact]
    public async Task OccurrenceRepository_Update_UpdatesFields()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateOccurrenceRepository();
        var occurrence = CreateOccurrence();
        await repo.CreateAsync(occurrence);

        // Act
        occurrence.Initialize(
            occurrence.StartTime.AddHours(1),
            TimeSpan.FromMinutes(45));
        occurrence.Extensions = new Dictionary<string, string> { ["updated"] = "true" };
        await repo.UpdateAsync(occurrence);

        var result = await repo.GetByIdAsync(occurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromMinutes(45), result!.Duration);
        Assert.True(result.Extensions?.ContainsKey("updated") == true);
    }

    [SkippableFact]
    public async Task OccurrenceRepository_Delete_RemovesOccurrence()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateOccurrenceRepository();
        var occurrence = CreateOccurrence();
        await repo.CreateAsync(occurrence);

        // Act
        await repo.DeleteAsync(occurrence.Id, TestOrganization, TestResourcePath);
        var result = await repo.GetByIdAsync(occurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.Null(result);
    }

    [SkippableFact]
    public async Task OccurrenceRepository_GetInRange_ReturnsOccurrencesInRange()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateOccurrenceRepository();
        var occurrence = CreateOccurrence();
        await repo.CreateAsync(occurrence);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await repo.GetInRangeAsync(
            TestOrganization, TestResourcePath, queryStart, queryEnd, null).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(occurrence.Id, results[0].Id);
    }

    #endregion

    #region Exception Repository Tests

    [SkippableFact]
    public async Task ExceptionRepository_CreateAndGetById_ReturnsCreatedException()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = CreateRecurrenceRepository();
        var exceptionRepo = CreateExceptionRepository();

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var exception = CreateException(recurrence.Id);

        // Act
        await exceptionRepo.CreateAsync(exception);
        var result = await exceptionRepo.GetByIdAsync(
            exception.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(recurrence.Id, result!.RecurrenceId);
        Assert.Equal(exception.OriginalTimeUtc, result.OriginalTimeUtc);
    }

    [SkippableFact]
    public async Task ExceptionRepository_GetByRecurrenceIds_ReturnsExceptionsForRecurrence()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = CreateRecurrenceRepository();
        var exceptionRepo = CreateExceptionRepository();

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var exception = CreateException(recurrence.Id);
        await exceptionRepo.CreateAsync(exception);

        // Act
        var results = await exceptionRepo.GetByRecurrenceIdsAsync(
            TestOrganization, TestResourcePath, [recurrence.Id]).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(recurrence.Id, results[0].RecurrenceId);
    }

    [SkippableFact]
    public async Task ExceptionRepository_Delete_RemovesException()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = CreateRecurrenceRepository();
        var exceptionRepo = CreateExceptionRepository();

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var exception = CreateException(recurrence.Id);
        await exceptionRepo.CreateAsync(exception);

        // Act
        await exceptionRepo.DeleteAsync(exception.Id, TestOrganization, TestResourcePath);
        var result = await exceptionRepo.GetByIdAsync(
            exception.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Override Repository Tests

    [SkippableFact]
    public async Task OverrideRepository_CreateAndGetById_ReturnsCreatedOverride()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = CreateRecurrenceRepository();
        var overrideRepo = CreateOverrideRepository();

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var @override = CreateOverride(recurrence.Id);

        // Act
        await overrideRepo.CreateAsync(@override);
        var result = await overrideRepo.GetByIdAsync(@override.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(recurrence.Id, result!.RecurrenceId);
        Assert.Equal(@override.OriginalTimeUtc, result.OriginalTimeUtc);
        Assert.Equal(@override.StartTime, result.StartTime);
        Assert.Equal(@override.OriginalDuration, result.OriginalDuration);
    }

    [SkippableFact]
    public async Task OverrideRepository_GetInRange_ReturnsOverridesWithOriginalTimeInRange()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = CreateRecurrenceRepository();
        var overrideRepo = CreateOverrideRepository();

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var @override = CreateOverride(recurrence.Id);
        await overrideRepo.CreateAsync(@override);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await overrideRepo.GetInRangeAsync(
            TestOrganization, TestResourcePath, [recurrence.Id], queryStart, queryEnd).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(recurrence.Id, results[0].RecurrenceId);
    }

    [SkippableFact]
    public async Task OverrideRepository_Update_UpdatesFields()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = CreateRecurrenceRepository();
        var overrideRepo = CreateOverrideRepository();

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var @override = CreateOverride(recurrence.Id);
        await overrideRepo.CreateAsync(@override);

        // Act
        @override.Initialize(@override.StartTime.AddHours(2), TimeSpan.FromMinutes(90));
        @override.Extensions = new Dictionary<string, string> { ["updated"] = "true" };
        await overrideRepo.UpdateAsync(@override);

        var result = await overrideRepo.GetByIdAsync(@override.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromMinutes(90), result!.Duration);
        Assert.True(result.Extensions?.ContainsKey("updated") == true);
    }

    #endregion

    #region Cascade Delete Tests

    [SkippableFact]
    public async Task RecurrenceRepository_Delete_CascadesDeleteToExceptionsAndOverrides()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = CreateRecurrenceRepository();
        var exceptionRepo = CreateExceptionRepository();
        var overrideRepo = CreateOverrideRepository();

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var exception = CreateException(recurrence.Id);
        await exceptionRepo.CreateAsync(exception);

        var @override = CreateOverride(recurrence.Id);
        await overrideRepo.CreateAsync(@override);

        // Act
        await recurrenceRepo.DeleteAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert - All related records should be deleted via CASCADE
        var recurrenceResult = await recurrenceRepo.GetByIdAsync(
            recurrence.Id, TestOrganization, TestResourcePath);
        Assert.Null(recurrenceResult);

        var exceptionResult = await exceptionRepo.GetByIdAsync(
            exception.Id, TestOrganization, TestResourcePath);
        Assert.Null(exceptionResult);

        var overrideResult = await overrideRepo.GetByIdAsync(
            @override.Id, TestOrganization, TestResourcePath);
        Assert.Null(overrideResult);
    }

    #endregion

    #region Extensions Tests

    [SkippableFact]
    public async Task RecurrenceRepository_Extensions_StoredAndRetrievedCorrectly()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateRecurrenceRepository();
        var recurrence = CreateRecurrence();
        recurrence.Extensions = new Dictionary<string, string>
        {
            ["title"] = "Test Meeting",
            ["location"] = "Conference Room A",
            ["unicode"] = "Hello \u4e16\u754c"
        };

        // Act
        await repo.CreateAsync(recurrence);
        var result = await repo.GetByIdAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.Extensions);
        Assert.Equal(3, result.Extensions!.Count);
        Assert.Equal("Test Meeting", result.Extensions["title"]);
        Assert.Equal("Conference Room A", result.Extensions["location"]);
        Assert.Equal("Hello \u4e16\u754c", result.Extensions["unicode"]);
    }

    #endregion

    #region Duration Tests

    [SkippableFact]
    public async Task Duration_StoredAsIntervalAndRetrievedCorrectly()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = CreateRecurrenceRepository();
        var recurrence = CreateRecurrence();
        recurrence.Duration = TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(15);

        // Act
        await repo.CreateAsync(recurrence);
        var result = await repo.GetByIdAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(15), result!.Duration);
    }

    #endregion

    #region Index Tests

    [SkippableFact]
    public async Task Schema_HasExpectedIndexes()
    {
        SkipIfNoConnection();

        // Arrange
        await using var connection = new NpgsqlConnection(_testConnectionString);
        await connection.OpenAsync();

        // Act - Query index names
        var sql = """
            SELECT indexname FROM pg_indexes
            WHERE schemaname = 'public'
            AND tablename IN ('recurrences', 'occurrences', 'occurrence_exceptions', 'occurrence_overrides')
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var indexNames = new List<string>();
        while (await reader.ReadAsync())
        {
            indexNames.Add(reader.GetString(0));
        }

        // Assert - Check for key indexes
        Assert.Contains("idx_recurrences_query", indexNames);
        Assert.Contains("idx_occurrences_query", indexNames);
        Assert.Contains("idx_exceptions_recurrence", indexNames);
        Assert.Contains("idx_exceptions_query", indexNames);
        Assert.Contains("idx_overrides_recurrence", indexNames);
        Assert.Contains("idx_overrides_original", indexNames);
        Assert.Contains("idx_overrides_start", indexNames);
    }

    #endregion

    #region Helper Methods

    private static Recurrence CreateRecurrence()
    {
        return new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            RRule = "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20251231T235959Z",
            TimeZone = TestTimeZone,
            Extensions = new Dictionary<string, string> { ["key"] = "value" }
        };
    }

    private static Occurrence CreateOccurrence()
    {
        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            Extensions = new Dictionary<string, string> { ["key"] = "value" }
        };
        occurrence.Initialize(
            new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromMinutes(30));
        return occurrence;
    }

    private static OccurrenceException CreateException(Guid recurrenceId)
    {
        return new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = new DateTime(2024, 6, 15, 9, 0, 0, DateTimeKind.Utc),
            Extensions = null
        };
    }

    private static OccurrenceOverride CreateOverride(Guid recurrenceId)
    {
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = new DateTime(2024, 6, 20, 9, 0, 0, DateTimeKind.Utc),
            OriginalDuration = TimeSpan.FromHours(1),
            OriginalExtensions = new Dictionary<string, string> { ["original"] = "true" },
            Extensions = new Dictionary<string, string> { ["modified"] = "true" }
        };
        @override.Initialize(
            new DateTime(2024, 6, 20, 10, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromMinutes(45));
        return @override;
    }

    #endregion
}
