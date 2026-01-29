namespace RecurringThings.Tests.Engine;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RecurringThings.Domain;
using RecurringThings.Engine;
using RecurringThings.Models;
using RecurringThings.Repository;
using Xunit;

/// <summary>
/// Unit tests for RecurrenceEngine.GetRecurrenceAsync and GetOccurrenceAsync methods.
/// </summary>
public class RecurrenceEngineGetByIdTests
{
    private readonly Mock<IRecurrenceRepository> _recurrenceRepo;
    private readonly Mock<IOccurrenceRepository> _occurrenceRepo;
    private readonly Mock<IOccurrenceExceptionRepository> _exceptionRepo;
    private readonly Mock<IOccurrenceOverrideRepository> _overrideRepo;
    private readonly RecurrenceEngine _engine;

    private const string TestOrganization = "test-org";
    private const string TestResourcePath = "test/path";
    private const string TestType = "appointment";
    private const string TestTimeZone = "America/New_York";
    private const string TestRRule = "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20251231T235959Z";

    public RecurrenceEngineGetByIdTests()
    {
        _recurrenceRepo = new Mock<IRecurrenceRepository>();
        _occurrenceRepo = new Mock<IOccurrenceRepository>();
        _exceptionRepo = new Mock<IOccurrenceExceptionRepository>();
        _overrideRepo = new Mock<IOccurrenceOverrideRepository>();

        _engine = new RecurrenceEngine(
            _recurrenceRepo.Object,
            _occurrenceRepo.Object,
            _exceptionRepo.Object,
            _overrideRepo.Object);
    }

    #region GetRecurrenceAsync Tests

    [Fact]
    public async Task GetRecurrenceAsync_ExistingRecurrence_ReturnsCalendarEntry()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var recurrence = CreateRecurrence(recurrenceId);

        _recurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, recurrenceId, null, default))
            .ReturnsAsync(recurrence);

        // Act
        var result = await _engine.GetRecurrenceAsync(TestOrganization, recurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(recurrenceId, result.RecurrenceId);
        Assert.Equal(CalendarEntryType.Recurrence, result.EntryType);
        Assert.Equal(TestOrganization, result.Organization);
        Assert.Equal(TestResourcePath, result.ResourcePath);
        Assert.Equal(TestType, result.Type);
    }

    [Fact]
    public async Task GetRecurrenceAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();

        _recurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, recurrenceId, null, default))
            .ReturnsAsync((Recurrence?)null);

        // Act
        var result = await _engine.GetRecurrenceAsync(TestOrganization, recurrenceId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecurrenceAsync_WrongOrganization_ReturnsNull()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var wrongOrganization = "wrong-org";

        _recurrenceRepo
            .Setup(r => r.GetAsync(wrongOrganization, recurrenceId, null, default))
            .ReturnsAsync((Recurrence?)null);

        // Act
        var result = await _engine.GetRecurrenceAsync(wrongOrganization, recurrenceId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecurrenceAsync_PopulatesRecurrenceDetails()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var recurrence = CreateRecurrence(recurrenceId);

        _recurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, recurrenceId, null, default))
            .ReturnsAsync(recurrence);

        // Act
        var result = await _engine.GetRecurrenceAsync(TestOrganization, recurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.RecurrenceDetails);
        Assert.Equal(TestRRule, result.RecurrenceDetails!.RRule);
    }

    [Fact]
    public async Task GetRecurrenceAsync_SetsEntryTypeToRecurrence()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var recurrence = CreateRecurrence(recurrenceId);

        _recurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, recurrenceId, null, default))
            .ReturnsAsync(recurrence);

        // Act
        var result = await _engine.GetRecurrenceAsync(TestOrganization, recurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CalendarEntryType.Recurrence, result.EntryType);
        Assert.Null(result.OccurrenceId);
        Assert.Null(result.OverrideId);
    }

    [Fact]
    public async Task GetRecurrenceAsync_ConvertsTimesToLocalTimezone()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var recurrence = CreateRecurrence(recurrenceId);

        _recurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, recurrenceId, null, default))
            .ReturnsAsync(recurrence);

        // Act
        var result = await _engine.GetRecurrenceAsync(TestOrganization, recurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestTimeZone, result.TimeZone);
        // StartTime is in UTC in domain, converted to local in CalendarEntry
        // We verify it's not UTC by checking the value differs (timezone conversion applied)
        Assert.NotEqual(DateTimeKind.Utc, result.StartTime.Kind);
    }

    [Fact]
    public async Task GetRecurrenceAsync_PopulatesExtensions()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var recurrence = CreateRecurrence(recurrenceId);
        recurrence.Extensions = new Dictionary<string, string> { ["key"] = "value" };

        _recurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, recurrenceId, null, default))
            .ReturnsAsync(recurrence);

        // Act
        var result = await _engine.GetRecurrenceAsync(TestOrganization, recurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Extensions);
        Assert.Equal("value", result.Extensions!["key"]);
    }

    [Fact]
    public async Task GetRecurrenceAsync_ComputesEndTimeFromDuration()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var recurrence = CreateRecurrence(recurrenceId);

        _recurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, recurrenceId, null, default))
            .ReturnsAsync(recurrence);

        // Act
        var result = await _engine.GetRecurrenceAsync(TestOrganization, recurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(recurrence.Duration, result.Duration);
        Assert.Equal(result.StartTime + result.Duration, result.EndTime);
    }

    #endregion

    #region GetOccurrenceAsync Tests

    [Fact]
    public async Task GetOccurrenceAsync_ExistingOccurrence_ReturnsCalendarEntry()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var occurrence = CreateOccurrence(occurrenceId);

        _occurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, occurrenceId, null, default))
            .ReturnsAsync(occurrence);

        // Act
        var result = await _engine.GetOccurrenceAsync(TestOrganization, occurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(occurrenceId, result.OccurrenceId);
        Assert.Equal(CalendarEntryType.Standalone, result.EntryType);
        Assert.Equal(TestOrganization, result.Organization);
        Assert.Equal(TestResourcePath, result.ResourcePath);
        Assert.Equal(TestType, result.Type);
    }

    [Fact]
    public async Task GetOccurrenceAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();

        _occurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, occurrenceId, null, default))
            .ReturnsAsync((Occurrence?)null);

        // Act
        var result = await _engine.GetOccurrenceAsync(TestOrganization, occurrenceId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOccurrenceAsync_WrongOrganization_ReturnsNull()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var wrongOrganization = "wrong-org";

        _occurrenceRepo
            .Setup(r => r.GetAsync(wrongOrganization, occurrenceId, null, default))
            .ReturnsAsync((Occurrence?)null);

        // Act
        var result = await _engine.GetOccurrenceAsync(wrongOrganization, occurrenceId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOccurrenceAsync_SetsEntryTypeToStandalone()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var occurrence = CreateOccurrence(occurrenceId);

        _occurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, occurrenceId, null, default))
            .ReturnsAsync(occurrence);

        // Act
        var result = await _engine.GetOccurrenceAsync(TestOrganization, occurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CalendarEntryType.Standalone, result.EntryType);
        Assert.Null(result.RecurrenceId);
        Assert.Null(result.OverrideId);
    }

    [Fact]
    public async Task GetOccurrenceAsync_SetsOccurrenceId()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var occurrence = CreateOccurrence(occurrenceId);

        _occurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, occurrenceId, null, default))
            .ReturnsAsync(occurrence);

        // Act
        var result = await _engine.GetOccurrenceAsync(TestOrganization, occurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(occurrenceId, result.OccurrenceId);
    }

    [Fact]
    public async Task GetOccurrenceAsync_ConvertsTimesToLocalTimezone()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var occurrence = CreateOccurrence(occurrenceId);

        _occurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, occurrenceId, null, default))
            .ReturnsAsync(occurrence);

        // Act
        var result = await _engine.GetOccurrenceAsync(TestOrganization, occurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestTimeZone, result.TimeZone);
        // StartTime is in UTC in domain, converted to local in CalendarEntry
        Assert.NotEqual(DateTimeKind.Utc, result.StartTime.Kind);
    }

    [Fact]
    public async Task GetOccurrenceAsync_PopulatesExtensions()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var occurrence = CreateOccurrence(occurrenceId);
        occurrence.Extensions = new Dictionary<string, string> { ["key"] = "value" };

        _occurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, occurrenceId, null, default))
            .ReturnsAsync(occurrence);

        // Act
        var result = await _engine.GetOccurrenceAsync(TestOrganization, occurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Extensions);
        Assert.Equal("value", result.Extensions!["key"]);
    }

    [Fact]
    public async Task GetOccurrenceAsync_HasNoOriginalDetails()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var occurrence = CreateOccurrence(occurrenceId);

        _occurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, occurrenceId, null, default))
            .ReturnsAsync(occurrence);

        // Act
        var result = await _engine.GetOccurrenceAsync(TestOrganization, occurrenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Original);
        Assert.False(result.IsOverridden);
    }

    [Fact]
    public async Task GetOccurrenceAsync_PassesCancellationToken()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _occurrenceRepo
            .Setup(r => r.GetAsync(TestOrganization, occurrenceId, null, token))
            .ReturnsAsync((Occurrence?)null);

        // Act
        await _engine.GetOccurrenceAsync(TestOrganization, occurrenceId, token);

        // Assert
        _occurrenceRepo.Verify(r => r.GetAsync(TestOrganization, occurrenceId, null, token), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Recurrence CreateRecurrence(Guid id)
    {
        return new Recurrence
        {
            Id = id,
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTime = new DateTime(2024, 1, 1, 14, 0, 0, DateTimeKind.Utc), // 9 AM EST
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            RRule = TestRRule,
            TimeZone = TestTimeZone,
            Extensions = null
        };
    }

    private static Occurrence CreateOccurrence(Guid id)
    {
        var occ = new Occurrence
        {
            Id = id,
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            Extensions = null
        };
        occ.Initialize(new DateTime(2024, 1, 15, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(30)); // 10 AM EST
        return occ;
    }

    #endregion
}
