namespace RecurringThings.MongoDB.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using global::MongoDB.Driver;
using RecurringThings.Domain;
using RecurringThings.MongoDB.Documents;
using RecurringThings.MongoDB.Indexing;
using RecurringThings.MongoDB.Repositories;
using Xunit;

/// <summary>
/// Integration tests for MongoDB repositories.
/// </summary>
/// <remarks>
/// Requires MONGODB_CONNECTION_STRING environment variable to be set.
/// Tests are skipped if the environment variable is not available.
/// </remarks>
[Trait("Category", "Integration")]
public class MongoDbIntegrationTests : IAsyncLifetime
{
    private readonly string? _connectionString;
    private readonly string _databaseName;
    private readonly string _collectionName = "recurring_things";
    private IMongoClient? _client;
    private IMongoDatabase? _database;

    private const string TestOrganization = "test-org";
    private const string TestResourcePath = "test/path";
    private const string TestType = "appointment";
    private const string TestTimeZone = "America/New_York";

    public MongoDbIntegrationTests()
    {
        _connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
        // MongoDB database name limit is 63 characters
        // Format: rt_test_yyyyMMddHHmmss_<8 chars of guid> = 8 + 14 + 1 + 8 = 31 chars
        _databaseName = $"rt_test_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..40];
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            return;
        }

        _client = new MongoClient(_connectionString);
        _database = _client.GetDatabase(_databaseName);

        // Create indexes
        var indexManager = new IndexManager(_database, _collectionName);
        await indexManager.ForceCreateIndexesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    private void SkipIfNoConnection()
    {
        Skip.If(string.IsNullOrEmpty(_connectionString),
            "MONGODB_CONNECTION_STRING environment variable not set");
    }

    #region Recurrence Repository Tests

    [SkippableFact]
    public async Task RecurrenceRepository_CreateAndGetById_ReturnsCreatedRecurrence()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = new MongoRecurrenceRepository(_database!, _collectionName);
        var recurrence = CreateRecurrence();

        // Act
        await repo.CreateAsync(recurrence);
        var result = await repo.GetByIdAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(recurrence.Id);
        result.Organization.Should().Be(TestOrganization);
        result.ResourcePath.Should().Be(TestResourcePath);
        result.Type.Should().Be(TestType);
        result.Duration.Should().Be(recurrence.Duration);
        result.Extensions.Should().BeEquivalentTo(recurrence.Extensions);
    }

    [SkippableFact]
    public async Task RecurrenceRepository_Update_UpdatesDurationAndExtensions()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = new MongoRecurrenceRepository(_database!, _collectionName);
        var recurrence = CreateRecurrence();
        await repo.CreateAsync(recurrence);

        // Act
        recurrence.Duration = TimeSpan.FromHours(2);
        recurrence.Extensions = new Dictionary<string, string> { ["updated"] = "true" };
        await repo.UpdateAsync(recurrence);

        var result = await repo.GetByIdAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        result.Should().NotBeNull();
        result!.Duration.Should().Be(TimeSpan.FromHours(2));
        result.Extensions.Should().ContainKey("updated");
    }

    [SkippableFact]
    public async Task RecurrenceRepository_Delete_RemovesRecurrence()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = new MongoRecurrenceRepository(_database!, _collectionName);
        var recurrence = CreateRecurrence();
        await repo.CreateAsync(recurrence);

        // Act
        await repo.DeleteAsync(recurrence.Id, TestOrganization, TestResourcePath);
        var result = await repo.GetByIdAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task RecurrenceRepository_GetInRange_ReturnsRecurrencesInRange()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = new MongoRecurrenceRepository(_database!, _collectionName);
        var recurrence = CreateRecurrence();
        await repo.CreateAsync(recurrence);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await repo.GetInRangeAsync(
            TestOrganization, TestResourcePath, queryStart, queryEnd, null).ToListAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].Id.Should().Be(recurrence.Id);
    }

    [SkippableFact]
    public async Task RecurrenceRepository_GetInRange_FiltersOutOfRange()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = new MongoRecurrenceRepository(_database!, _collectionName);
        var recurrence = CreateRecurrence();
        await repo.CreateAsync(recurrence);

        // Query range before recurrence
        var queryStart = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2020, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await repo.GetInRangeAsync(
            TestOrganization, TestResourcePath, queryStart, queryEnd, null).ToListAsync();

        // Assert
        results.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task RecurrenceRepository_GetInRange_FiltersByType()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = new MongoRecurrenceRepository(_database!, _collectionName);
        var recurrence1 = CreateRecurrence();
        var recurrence2 = CreateRecurrence();

        // Change type on second recurrence (need to create new instance since Type is init-only)
        var recurrence2Modified = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = "meeting",
            StartTime = recurrence2.StartTime,
            Duration = recurrence2.Duration,
            RecurrenceEndTime = recurrence2.RecurrenceEndTime,
            RRule = recurrence2.RRule,
            TimeZone = recurrence2.TimeZone,
            Extensions = recurrence2.Extensions
        };

        await repo.CreateAsync(recurrence1);
        await repo.CreateAsync(recurrence2Modified);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await repo.GetInRangeAsync(
            TestOrganization, TestResourcePath, queryStart, queryEnd, [TestType]).ToListAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].Type.Should().Be(TestType);
    }

    #endregion

    #region Occurrence Repository Tests

    [SkippableFact]
    public async Task OccurrenceRepository_CreateAndGetById_ReturnsCreatedOccurrence()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = new MongoOccurrenceRepository(_database!, _collectionName);
        var occurrence = CreateOccurrence();

        // Act
        await repo.CreateAsync(occurrence);
        var result = await repo.GetByIdAsync(occurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(occurrence.Id);
        result.StartTime.Should().Be(occurrence.StartTime);
        result.EndTime.Should().Be(occurrence.EndTime);
        result.Duration.Should().Be(occurrence.Duration);
    }

    [SkippableFact]
    public async Task OccurrenceRepository_Update_UpdatesFields()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = new MongoOccurrenceRepository(_database!, _collectionName);
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
        result.Should().NotBeNull();
        result!.Duration.Should().Be(TimeSpan.FromMinutes(45));
        result.Extensions.Should().ContainKey("updated");
    }

    #endregion

    #region Exception Repository Tests

    [SkippableFact]
    public async Task ExceptionRepository_CreateAndGetById_ReturnsCreatedException()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = new MongoRecurrenceRepository(_database!, _collectionName);
        var exceptionRepo = new MongoOccurrenceExceptionRepository(_database!, _collectionName);

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var exception = CreateException(recurrence.Id);

        // Act
        await exceptionRepo.CreateAsync(exception);
        var result = await exceptionRepo.GetByIdAsync(
            exception.Id, TestOrganization, TestResourcePath);

        // Assert
        result.Should().NotBeNull();
        result!.RecurrenceId.Should().Be(recurrence.Id);
        result.OriginalTimeUtc.Should().Be(exception.OriginalTimeUtc);
    }

    [SkippableFact]
    public async Task ExceptionRepository_GetByRecurrenceIds_ReturnsExceptionsForRecurrence()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = new MongoRecurrenceRepository(_database!, _collectionName);
        var exceptionRepo = new MongoOccurrenceExceptionRepository(_database!, _collectionName);

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var exception = CreateException(recurrence.Id);
        await exceptionRepo.CreateAsync(exception);

        // Act
        var results = await exceptionRepo.GetByRecurrenceIdsAsync(
            TestOrganization, TestResourcePath, [recurrence.Id]).ToListAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].RecurrenceId.Should().Be(recurrence.Id);
    }

    #endregion

    #region Override Repository Tests

    [SkippableFact]
    public async Task OverrideRepository_CreateAndGetById_ReturnsCreatedOverride()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = new MongoRecurrenceRepository(_database!, _collectionName);
        var overrideRepo = new MongoOccurrenceOverrideRepository(_database!, _collectionName);

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var @override = CreateOverride(recurrence.Id);

        // Act
        await overrideRepo.CreateAsync(@override);
        var result = await overrideRepo.GetByIdAsync(@override.Id, TestOrganization, TestResourcePath);

        // Assert
        result.Should().NotBeNull();
        result!.RecurrenceId.Should().Be(recurrence.Id);
        result.OriginalTimeUtc.Should().Be(@override.OriginalTimeUtc);
        result.StartTime.Should().Be(@override.StartTime);
        result.OriginalDuration.Should().Be(@override.OriginalDuration);
    }

    [SkippableFact]
    public async Task OverrideRepository_GetInRange_ReturnsOverridesWithOriginalTimeInRange()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = new MongoRecurrenceRepository(_database!, _collectionName);
        var overrideRepo = new MongoOccurrenceOverrideRepository(_database!, _collectionName);

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
        results.Should().HaveCount(1);
        results[0].RecurrenceId.Should().Be(recurrence.Id);
    }

    #endregion

    #region Cascade Delete Tests

    [SkippableFact]
    public async Task RecurrenceRepository_Delete_CascadesDeleteToExceptionsAndOverrides()
    {
        SkipIfNoConnection();

        // Arrange
        var recurrenceRepo = new MongoRecurrenceRepository(_database!, _collectionName);
        var exceptionRepo = new MongoOccurrenceExceptionRepository(_database!, _collectionName);
        var overrideRepo = new MongoOccurrenceOverrideRepository(_database!, _collectionName);

        var recurrence = CreateRecurrence();
        await recurrenceRepo.CreateAsync(recurrence);

        var exception = CreateException(recurrence.Id);
        await exceptionRepo.CreateAsync(exception);

        var @override = CreateOverride(recurrence.Id);
        await overrideRepo.CreateAsync(@override);

        // Act
        await recurrenceRepo.DeleteAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert - All related documents should be deleted
        var recurrenceResult = await recurrenceRepo.GetByIdAsync(
            recurrence.Id, TestOrganization, TestResourcePath);
        recurrenceResult.Should().BeNull();

        var exceptionResult = await exceptionRepo.GetByIdAsync(
            exception.Id, TestOrganization, TestResourcePath);
        exceptionResult.Should().BeNull();

        var overrideResult = await overrideRepo.GetByIdAsync(
            @override.Id, TestOrganization, TestResourcePath);
        overrideResult.Should().BeNull();
    }

    #endregion

    #region Index Tests

    [SkippableFact]
    public async Task IndexManager_EnsureIndexes_CreatesExpectedIndexes()
    {
        SkipIfNoConnection();

        // Arrange
        var collection = _database!.GetCollection<RecurringThingDocument>(_collectionName);

        // Act - indexes were created in InitializeAsync
        var indexes = await collection.Indexes.ListAsync();
        var indexList = await indexes.ToListAsync();

        // Assert - should have at least 5 indexes (_id + 4 custom)
        indexList.Should().HaveCountGreaterThanOrEqualTo(5);

        var indexNames = indexList.Select(i => i["name"].AsString).ToList();
        indexNames.Should().Contain("idx_recurring_query");
        indexNames.Should().Contain("idx_original_time");
        indexNames.Should().Contain("idx_override_time_range");
        indexNames.Should().Contain("idx_cascade_delete");
    }

    #endregion

    #region Extensions Tests

    [SkippableFact]
    public async Task RecurrenceRepository_Extensions_StoredAndRetrievedCorrectly()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = new MongoRecurrenceRepository(_database!, _collectionName);
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
        result.Should().NotBeNull();
        result!.Extensions.Should().NotBeNull();
        result.Extensions.Should().HaveCount(3);
        result.Extensions!["title"].Should().Be("Test Meeting");
        result.Extensions["location"].Should().Be("Conference Room A");
        result.Extensions["unicode"].Should().Be("Hello \u4e16\u754c");
    }

    #endregion

    #region Duration Tests

    [SkippableFact]
    public async Task Duration_StoredAsMillisecondsAndRetrievedCorrectly()
    {
        SkipIfNoConnection();

        // Arrange
        var repo = new MongoRecurrenceRepository(_database!, _collectionName);
        var recurrence = CreateRecurrence();
        recurrence.Duration = TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(15);

        // Act
        await repo.CreateAsync(recurrence);
        var result = await repo.GetByIdAsync(recurrence.Id, TestOrganization, TestResourcePath);

        // Assert
        result.Should().NotBeNull();
        result!.Duration.Should().Be(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(15));
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
