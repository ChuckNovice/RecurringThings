namespace RecurringThings.Tests.Engine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RecurringThings.Domain;
using RecurringThings.Engine;
using RecurringThings.Models;
using RecurringThings.Repository;
using Transactional.Abstractions;
using Xunit;

public class RecurrenceEngineTests
{
    private readonly Mock<IRecurrenceRepository> _recurrenceRepo;
    private readonly Mock<IOccurrenceRepository> _occurrenceRepo;
    private readonly Mock<IOccurrenceExceptionRepository> _exceptionRepo;
    private readonly Mock<IOccurrenceOverrideRepository> _overrideRepo;
    private readonly RecurrenceEngine _engine;

    private const string TestOrganization = "test-org";
    private const string TestResourcePath = "test/path";
    private const string TestType = "appointment";
    private const string TestTimeZone = "Etc/UTC";

    public RecurrenceEngineTests()
    {
        _recurrenceRepo = new Mock<IRecurrenceRepository>();
        _occurrenceRepo = new Mock<IOccurrenceRepository>();
        _exceptionRepo = new Mock<IOccurrenceExceptionRepository>();
        _overrideRepo = new Mock<IOccurrenceOverrideRepository>();

        // Default empty results
        SetupEmptyRepositories();

        _engine = new RecurrenceEngine(
            _recurrenceRepo.Object,
            _occurrenceRepo.Object,
            _exceptionRepo.Object,
            _overrideRepo.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRecurrenceRepository_ThrowsArgumentNullException()
    {
        var act = () => new RecurrenceEngine(
            null!,
            _occurrenceRepo.Object,
            _exceptionRepo.Object,
            _overrideRepo.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("recurrenceRepository");
    }

    [Fact]
    public void Constructor_WithNullOccurrenceRepository_ThrowsArgumentNullException()
    {
        var act = () => new RecurrenceEngine(
            _recurrenceRepo.Object,
            null!,
            _exceptionRepo.Object,
            _overrideRepo.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("occurrenceRepository");
    }

    [Fact]
    public void Constructor_WithNullExceptionRepository_ThrowsArgumentNullException()
    {
        var act = () => new RecurrenceEngine(
            _recurrenceRepo.Object,
            _occurrenceRepo.Object,
            null!,
            _overrideRepo.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exceptionRepository");
    }

    [Fact]
    public void Constructor_WithNullOverrideRepository_ThrowsArgumentNullException()
    {
        var act = () => new RecurrenceEngine(
            _recurrenceRepo.Object,
            _occurrenceRepo.Object,
            _exceptionRepo.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("overrideRepository");
    }

    #endregion

    #region Basic Recurrence Generation Tests

    [Fact]
    public async Task GetAsync_DailyRecurrence_ReturnsCorrectOccurrences()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240110T235959Z",
            new DateTime(2024, 1, 10, 23, 59, 59, DateTimeKind.Utc));

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r =>
        {
            r.RecurrenceId.Should().Be(recurrenceId);
            r.Duration.Should().Be(duration);
            r.Type.Should().Be(TestType);
            r.TimeZone.Should().Be(TestTimeZone);
            r.EntryType.Should().Be(CalendarEntryType.Virtualized);
            r.Original.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task GetAsync_WeeklyRecurrence_ReturnsCorrectOccurrences()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc); // Monday
        var duration = TimeSpan.FromHours(2);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=WEEKLY;BYDAY=MO,WE,FR;UNTIL=20240131T235959Z",
            new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc));

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 14, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCountGreaterThan(0);
        results.Should().AllSatisfy(r =>
        {
            r.RecurrenceId.Should().Be(recurrenceId);
            var dayOfWeek = r.StartTime.DayOfWeek;
            dayOfWeek.Should().BeOneOf(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        });
    }

    [Fact]
    public async Task GetAsync_MonthlyRecurrence_ReturnsCorrectOccurrences()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 15, 14, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(30);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=15;UNTIL=20240615T235959Z",
            new DateTime(2024, 6, 15, 23, 59, 59, DateTimeKind.Utc));

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 4, 30, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(4); // Jan 15, Feb 15, Mar 15, Apr 15
        results.Should().AllSatisfy(r => r.StartTime.Day.Should().Be(15));
    }

    [Fact]
    public async Task GetAsync_YearlyRecurrence_ReturnsCorrectOccurrences()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 3, 20, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(8);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=YEARLY;BYMONTH=3;BYMONTHDAY=20;UNTIL=20280320T235959Z",
            new DateTime(2028, 3, 20, 23, 59, 59, DateTimeKind.Utc));

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(3); // 2024, 2025, 2026
        results.Should().AllSatisfy(r =>
        {
            r.StartTime.Month.Should().Be(3);
            r.StartTime.Day.Should().Be(20);
        });
    }

    #endregion

    #region Exception Tests

    [Fact]
    public async Task GetAsync_WithException_ExcludesExceptedOccurrence()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240105T235959Z",
            new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc));

        // Exception on Jan 3
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = new DateTime(2024, 1, 3, 9, 0, 0, DateTimeKind.Utc)
        };

        SetupRecurrences([recurrence]);
        SetupExceptions([exception]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(4); // 5 days - 1 exception = 4
        results.Should().NotContain(r => r.StartTime.Day == 3);
    }

    [Fact]
    public async Task GetAsync_WithMultipleExceptions_ExcludesAllExceptedOccurrences()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240107T235959Z",
            new DateTime(2024, 1, 7, 23, 59, 59, DateTimeKind.Utc));

        var exceptions = new[]
        {
            new OccurrenceException
            {
                Id = Guid.NewGuid(),
                Organization = TestOrganization,
                ResourcePath = TestResourcePath,
                RecurrenceId = recurrenceId,
                OriginalTimeUtc = new DateTime(2024, 1, 2, 9, 0, 0, DateTimeKind.Utc)
            },
            new OccurrenceException
            {
                Id = Guid.NewGuid(),
                Organization = TestOrganization,
                ResourcePath = TestResourcePath,
                RecurrenceId = recurrenceId,
                OriginalTimeUtc = new DateTime(2024, 1, 5, 9, 0, 0, DateTimeKind.Utc)
            }
        };

        SetupRecurrences([recurrence]);
        SetupExceptions(exceptions);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 7, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(5); // 7 - 2 = 5
        results.Should().NotContain(r => r.StartTime.Day == 2);
        results.Should().NotContain(r => r.StartTime.Day == 5);
    }

    #endregion

    #region Override Tests

    [Fact]
    public async Task GetAsync_WithOverride_ReturnsOverriddenValues()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240105T235959Z",
            new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc));

        // Override on Jan 3: change start time and duration
        var overrideEntity = new OccurrenceOverride
        {
            Id = overrideId,
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = new DateTime(2024, 1, 3, 9, 0, 0, DateTimeKind.Utc),
            OriginalDuration = duration,
            Extensions = new Dictionary<string, string> { ["modified"] = "true" }
        };
        overrideEntity.Initialize(
            new DateTime(2024, 1, 3, 14, 0, 0, DateTimeKind.Utc), // Moved to 2 PM
            TimeSpan.FromHours(2)); // Extended to 2 hours

        SetupRecurrences([recurrence]);
        SetupOverrides([overrideEntity]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(5);

        var overriddenEntry = results.Single(r => r.OverrideId == overrideId);
        overriddenEntry.StartTime.Hour.Should().Be(14);
        overriddenEntry.Duration.Should().Be(TimeSpan.FromHours(2));
        overriddenEntry.Extensions.Should().ContainKey("modified");
        overriddenEntry.Original.Should().NotBeNull();
        overriddenEntry.Original!.StartTime.Should().Be(new DateTime(2024, 1, 3, 9, 0, 0, DateTimeKind.Utc));
        overriddenEntry.Original!.Duration.Should().Be(duration);
    }

    [Fact]
    public async Task GetAsync_WithOverrideMovedOutsideRange_ExcludesOverriddenOccurrence()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240110T235959Z",
            new DateTime(2024, 1, 10, 23, 59, 59, DateTimeKind.Utc));

        // Override moves Jan 3 occurrence to Jan 15 (outside query range)
        var overrideEntity = new OccurrenceOverride
        {
            Id = overrideId,
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = new DateTime(2024, 1, 3, 9, 0, 0, DateTimeKind.Utc),
            OriginalDuration = duration
        };
        overrideEntity.Initialize(
            new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc),
            duration);

        SetupRecurrences([recurrence]);
        SetupOverrides([overrideEntity]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        // Jan 1-5 = 5 occurrences, but Jan 3 was moved out, so 4 remain
        results.Should().HaveCount(4);
        results.Should().NotContain(r => r.StartTime.Day == 3);
    }

    [Fact]
    public async Task GetAsync_WithOverrideMovedIntoRange_IncludesOverriddenOccurrence()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240120T235959Z",
            new DateTime(2024, 1, 20, 23, 59, 59, DateTimeKind.Utc));

        // Override moves Jan 15 occurrence to Jan 3 (inside query range)
        var overrideEntity = new OccurrenceOverride
        {
            Id = overrideId,
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc),
            OriginalDuration = duration
        };
        overrideEntity.Initialize(
            new DateTime(2024, 1, 3, 14, 0, 0, DateTimeKind.Utc),
            duration);

        SetupRecurrences([recurrence]);
        SetupOverrides([overrideEntity]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        // Jan 1-5 = 5 regular occurrences + 1 moved-in override = 6 total
        results.Should().HaveCount(6);
        results.Should().Contain(r => r.OverrideId == overrideId);
    }

    #endregion

    #region Standalone Occurrence Tests

    [Fact]
    public async Task GetAsync_WithStandaloneOccurrences_MergesWithVirtualized()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240103T235959Z",
            new DateTime(2024, 1, 3, 23, 59, 59, DateTimeKind.Utc));

        var occurrence = new Occurrence
        {
            Id = occurrenceId,
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone
        };
        occurrence.Initialize(
            new DateTime(2024, 1, 2, 15, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromMinutes(30));

        SetupRecurrences([recurrence]);
        SetupOccurrences([occurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 3, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(4); // 3 virtualized + 1 standalone

        var standaloneEntry = results.Single(r => r.OccurrenceId == occurrenceId);
        standaloneEntry.RecurrenceId.Should().BeNull();
        standaloneEntry.EntryType.Should().Be(CalendarEntryType.Standalone);
        standaloneEntry.StartTime.Hour.Should().Be(15);
    }

    #endregion

    #region Type Filtering Tests

    [Fact]
    public async Task GetAsync_WithTypeFilter_ReturnsOnlyMatchingTypes()
    {
        // Arrange
        var recurrence1 = CreateRecurrence(
            Guid.NewGuid(),
            new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1),
            "FREQ=DAILY;UNTIL=20240105T235959Z",
            new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc),
            type: "appointment");

        var recurrence2 = CreateRecurrence(
            Guid.NewGuid(),
            new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1),
            "FREQ=DAILY;UNTIL=20240105T235959Z",
            new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc),
            type: "meeting");

        // Setup mock to return only appointment recurrences when filter is applied
        // (Repository is responsible for filtering)
        SetupRecurrences([recurrence1]); // Only appointment type

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 3, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd, types: ["appointment"]);

        // Assert
        results.Should().AllSatisfy(r => r.Type.Should().Be("appointment"));
    }

    [Fact]
    public async Task GetAsync_WithNullTypeFilter_ReturnsAllTypes()
    {
        // Arrange
        var recurrence1 = CreateRecurrence(
            Guid.NewGuid(),
            new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1),
            "FREQ=DAILY;UNTIL=20240103T235959Z",
            new DateTime(2024, 1, 3, 23, 59, 59, DateTimeKind.Utc),
            type: "appointment");

        var recurrence2 = CreateRecurrence(
            Guid.NewGuid(),
            new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1),
            "FREQ=DAILY;UNTIL=20240103T235959Z",
            new DateTime(2024, 1, 3, 23, 59, 59, DateTimeKind.Utc),
            type: "meeting");

        SetupRecurrences([recurrence1, recurrence2]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 3, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd, types: null);

        // Assert
        results.Should().Contain(r => r.Type == "appointment");
        results.Should().Contain(r => r.Type == "meeting");
    }

    [Fact]
    public async Task GetAsync_WithEmptyTypeFilter_ThrowsArgumentException()
    {
        // Arrange
        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var act = async () => await GetResultsAsync(queryStart, queryEnd, types: []);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Boundary Condition Tests

    [Fact]
    public async Task GetAsync_OccurrenceAtExactQueryStart_IsIncluded()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240105T235959Z",
            new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc));

        SetupRecurrences([recurrence]);

        // Query starts exactly at occurrence time
        var queryStart = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 1, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(1);
        results[0].StartTime.Should().Be(queryStart);
    }

    [Fact]
    public async Task GetAsync_OccurrenceAtExactQueryEnd_IsIncluded()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 23, 59, 0, DateTimeKind.Utc); // 23:59:00, not 23:59:59
        var duration = TimeSpan.FromMinutes(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240105T235959Z",
            new DateTime(2024, 1, 5, 23, 59, 59, DateTimeKind.Utc));

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 1, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(1);
        results[0].StartTime.Should().Be(startTime); // Occurrence at 23:59:00 is within the query range
    }

    [Fact]
    public async Task GetAsync_RecurrenceEndTimeFiltering_ExcludesOccurrencesBeyondEnd()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        // RecurrenceEndTime is Jan 3, but RRule goes to Jan 10
        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240110T235959Z",
            new DateTime(2024, 1, 3, 23, 59, 59, DateTimeKind.Utc));

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 10, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(3); // Only Jan 1-3 due to RecurrenceEndTime
    }

    #endregion

    #region Empty Results Tests

    [Fact]
    public async Task GetAsync_NoRecurrencesOrOccurrences_ReturnsEmpty()
    {
        // Arrange - defaults are empty
        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_RecurrenceOutsideRange_ReturnsEmpty()
    {
        // Arrange
        var recurrence = CreateRecurrence(
            Guid.NewGuid(),
            new DateTime(2024, 6, 1, 9, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1),
            "FREQ=DAILY;UNTIL=20240610T235959Z",
            new DateTime(2024, 6, 10, 23, 59, 59, DateTimeKind.Utc));

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region CalendarEntry Structure Tests

    [Fact]
    public async Task GetAsync_VirtualizedOccurrence_HasCorrectStructure()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);
        var extensions = new Dictionary<string, string> { ["key"] = "value" };

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=DAILY;UNTIL=20240102T235959Z",
            new DateTime(2024, 1, 2, 23, 59, 59, DateTimeKind.Utc),
            extensions: extensions);

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 1, 1, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        results.Should().HaveCount(1);
        var entry = results[0];

        entry.Organization.Should().Be(TestOrganization);
        entry.ResourcePath.Should().Be(TestResourcePath);
        entry.Type.Should().Be(TestType);
        entry.StartTime.Should().Be(startTime);
        entry.EndTime.Should().Be(startTime + duration);
        entry.Duration.Should().Be(duration);
        entry.TimeZone.Should().Be(TestTimeZone);
        entry.Extensions.Should().BeEquivalentTo(extensions);
        entry.RecurrenceId.Should().Be(recurrenceId);
        entry.OccurrenceId.Should().BeNull();
        entry.OverrideId.Should().BeNull();
        entry.ExceptionId.Should().BeNull();
        entry.EntryType.Should().Be(CalendarEntryType.Virtualized);
        entry.RecurrenceDetails.Should().NotBeNull();
        entry.RecurrenceDetails!.RRule.Should().NotBeNullOrEmpty();
        entry.Original.Should().NotBeNull();
        entry.Original!.StartTime.Should().Be(startTime);
        entry.Original.Duration.Should().Be(duration);
        entry.Original.Extensions.Should().BeEquivalentTo(extensions);
    }

    #endregion

    #region Helper Methods

    private void SetupEmptyRepositories()
    {
        _recurrenceRepo
            .Setup(r => r.GetInRangeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string[]?>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<Recurrence>());

        _occurrenceRepo
            .Setup(r => r.GetInRangeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string[]?>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<Occurrence>());

        _exceptionRepo
            .Setup(r => r.GetByRecurrenceIdsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<OccurrenceException>());

        _overrideRepo
            .Setup(r => r.GetInRangeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<OccurrenceOverride>());
    }

    private void SetupRecurrences(IEnumerable<Recurrence> recurrences)
    {
        _recurrenceRepo
            .Setup(r => r.GetInRangeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string[]?>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .Returns(recurrences.ToAsyncEnumerable());
    }

    private void SetupOccurrences(IEnumerable<Occurrence> occurrences)
    {
        _occurrenceRepo
            .Setup(r => r.GetInRangeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string[]?>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .Returns(occurrences.ToAsyncEnumerable());
    }

    private void SetupExceptions(IEnumerable<OccurrenceException> exceptions)
    {
        _exceptionRepo
            .Setup(r => r.GetByRecurrenceIdsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .Returns(exceptions.ToAsyncEnumerable());
    }

    private void SetupOverrides(IEnumerable<OccurrenceOverride> overrides)
    {
        _overrideRepo
            .Setup(r => r.GetInRangeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .Returns(overrides.ToAsyncEnumerable());
    }

    private static Recurrence CreateRecurrence(
        Guid id,
        DateTime startTime,
        TimeSpan duration,
        string rrule,
        DateTime recurrenceEndTime,
        string type = TestType,
        Dictionary<string, string>? extensions = null)
    {
        return new Recurrence
        {
            Id = id,
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = type,
            StartTime = startTime,
            Duration = duration,
            RecurrenceEndTime = recurrenceEndTime,
            RRule = rrule,
            TimeZone = TestTimeZone,
            Extensions = extensions
        };
    }

    private async Task<List<CalendarEntry>> GetResultsAsync(
        DateTime start,
        DateTime end,
        string[]? types = null)
    {
        var results = new List<CalendarEntry>();
        await foreach (var entry in _engine.GetOccurrencesAsync(
            TestOrganization,
            TestResourcePath,
            start,
            end,
            types))
        {
            results.Add(entry);
        }

        return results;
    }

    #endregion
}
