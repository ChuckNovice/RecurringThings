namespace RecurringThings.Tests.Engine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RecurringThings.Domain;
using RecurringThings.Engine;
using RecurringThings.Exceptions;
using RecurringThings.Models;
using RecurringThings.Options;
using RecurringThings.Repository;
using Transactional.Abstractions;
using Xunit;

/// <summary>
/// Tests for monthly recurrence out-of-bounds day handling.
/// </summary>
public class RecurrenceEngineMonthlyBoundsTests
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

    public RecurrenceEngineMonthlyBoundsTests()
    {
        _recurrenceRepo = new Mock<IRecurrenceRepository>();
        _occurrenceRepo = new Mock<IOccurrenceRepository>();
        _exceptionRepo = new Mock<IOccurrenceExceptionRepository>();
        _overrideRepo = new Mock<IOccurrenceOverrideRepository>();

        SetupEmptyRepositories();
        SetupCreateRecurrence();

        _engine = new RecurrenceEngine(
            _recurrenceRepo.Object,
            _occurrenceRepo.Object,
            _exceptionRepo.Object,
            _overrideRepo.Object);
    }

    #region Throw Strategy Tests

    [Fact]
    public async Task CreateRecurrenceAsync_Monthly31st_DefaultOptions_ThrowsException()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20241231T235959Z";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MonthDayOutOfBoundsException>(() =>
            _engine.CreateRecurrenceAsync(
                TestOrganization,
                TestResourcePath,
                TestType,
                startTime,
                TimeSpan.FromHours(1),
                rrule,
                TestTimeZone));

        Assert.Equal(31, exception.DayOfMonth);
        Assert.Contains(2, exception.AffectedMonths); // February
        Assert.Contains(4, exception.AffectedMonths); // April
        Assert.Contains(6, exception.AffectedMonths); // June
        Assert.Contains(9, exception.AffectedMonths); // September
        Assert.Contains(11, exception.AffectedMonths); // November
    }

    [Fact]
    public async Task CreateRecurrenceAsync_Monthly31st_ThrowStrategy_ThrowsWithAffectedMonths()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20240630T235959Z";
        var options = new CreateRecurrenceOptions { OutOfBoundsMonthBehavior = MonthDayOutOfBoundsStrategy.Throw };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MonthDayOutOfBoundsException>(() =>
            _engine.CreateRecurrenceAsync(
                TestOrganization,
                TestResourcePath,
                TestType,
                startTime,
                TimeSpan.FromHours(1),
                rrule,
                TestTimeZone,
                options: options));

        Assert.Equal(31, exception.DayOfMonth);
        Assert.Contains(2, exception.AffectedMonths); // February
        Assert.Contains(4, exception.AffectedMonths); // April
        Assert.Contains(6, exception.AffectedMonths); // June
    }

    [Fact]
    public async Task CreateRecurrenceAsync_Monthly30th_ThrowsForFebruary()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 30, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=MONTHLY;BYMONTHDAY=30;UNTIL=20240430T235959Z";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MonthDayOutOfBoundsException>(() =>
            _engine.CreateRecurrenceAsync(
                TestOrganization,
                TestResourcePath,
                TestType,
                startTime,
                TimeSpan.FromHours(1),
                rrule,
                TestTimeZone));

        Assert.Equal(30, exception.DayOfMonth);
        Assert.Contains(2, exception.AffectedMonths); // February doesn't have 30 days
    }

    [Fact]
    public async Task CreateRecurrenceAsync_Monthly29th_ThrowsForNonLeapYearFebruary()
    {
        // Arrange - 2025 is not a leap year
        var startTime = new DateTime(2025, 1, 29, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=MONTHLY;BYMONTHDAY=29;UNTIL=20250430T235959Z";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MonthDayOutOfBoundsException>(() =>
            _engine.CreateRecurrenceAsync(
                TestOrganization,
                TestResourcePath,
                TestType,
                startTime,
                TimeSpan.FromHours(1),
                rrule,
                TestTimeZone));

        Assert.Equal(29, exception.DayOfMonth);
        Assert.Contains(2, exception.AffectedMonths);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_Monthly28th_DoesNotThrow()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 28, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=MONTHLY;BYMONTHDAY=28;UNTIL=20241231T235959Z";

        // Act
        var result = await _engine.CreateRecurrenceAsync(
            TestOrganization,
            TestResourcePath,
            TestType,
            startTime,
            TimeSpan.FromHours(1),
            rrule,
            TestTimeZone);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.RecurrenceDetails?.MonthDayBehavior);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_DailyPattern_DoesNotValidateMonthlyBounds()
    {
        // Arrange - Daily pattern starting on 31st should not throw
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=DAILY;UNTIL=20241231T235959Z";

        // Act
        var result = await _engine.CreateRecurrenceAsync(
            TestOrganization,
            TestResourcePath,
            TestType,
            startTime,
            TimeSpan.FromHours(1),
            rrule,
            TestTimeZone);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.RecurrenceDetails?.MonthDayBehavior);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WeeklyPattern_DoesNotValidateMonthlyBounds()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=WEEKLY;BYDAY=MO,WE,FR;UNTIL=20241231T235959Z";

        // Act
        var result = await _engine.CreateRecurrenceAsync(
            TestOrganization,
            TestResourcePath,
            TestType,
            startTime,
            TimeSpan.FromHours(1),
            rrule,
            TestTimeZone);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.RecurrenceDetails?.MonthDayBehavior);
    }

    #endregion

    #region Skip Strategy Tests

    [Fact]
    public async Task CreateRecurrenceAsync_Monthly31st_SkipStrategy_CreatesRecurrence()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20241231T235959Z";
        var options = new CreateRecurrenceOptions { OutOfBoundsMonthBehavior = MonthDayOutOfBoundsStrategy.Skip };

        // Act
        var result = await _engine.CreateRecurrenceAsync(
            TestOrganization,
            TestResourcePath,
            TestType,
            startTime,
            TimeSpan.FromHours(1),
            rrule,
            TestTimeZone,
            options: options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CalendarEntryType.Recurrence, result.EntryType);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_SkipStrategy_StoresStrategyInRecurrence()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20241231T235959Z";
        var options = new CreateRecurrenceOptions { OutOfBoundsMonthBehavior = MonthDayOutOfBoundsStrategy.Skip };

        // Act
        var result = await _engine.CreateRecurrenceAsync(
            TestOrganization,
            TestResourcePath,
            TestType,
            startTime,
            TimeSpan.FromHours(1),
            rrule,
            TestTimeZone,
            options: options);

        // Assert
        Assert.Equal(MonthDayOutOfBoundsStrategy.Skip, result.RecurrenceDetails?.MonthDayBehavior);
    }

    [Fact]
    public async Task GetOccurrencesAsync_Monthly31st_SkipStrategy_SkipsShortMonths()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20240731T235959Z",
            new DateTime(2024, 7, 31, 23, 59, 59, DateTimeKind.Utc),
            monthDayBehavior: MonthDayOutOfBoundsStrategy.Skip);

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 7, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        // Jan, Mar, May, Jul have 31 days = 4 occurrences
        // Feb, Apr, Jun are skipped
        Assert.Equal(4, results.Count);
        Assert.All(results, r => Assert.Equal(31, r.StartTime.Day));
    }

    #endregion

    #region Clamp Strategy Tests

    [Fact]
    public async Task CreateRecurrenceAsync_Monthly31st_ClampStrategy_CreatesRecurrence()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20241231T235959Z";
        var options = new CreateRecurrenceOptions { OutOfBoundsMonthBehavior = MonthDayOutOfBoundsStrategy.Clamp };

        // Act
        var result = await _engine.CreateRecurrenceAsync(
            TestOrganization,
            TestResourcePath,
            TestType,
            startTime,
            TimeSpan.FromHours(1),
            rrule,
            TestTimeZone,
            options: options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CalendarEntryType.Recurrence, result.EntryType);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_ClampStrategy_StoresStrategyInRecurrence()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var rrule = "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20241231T235959Z";
        var options = new CreateRecurrenceOptions { OutOfBoundsMonthBehavior = MonthDayOutOfBoundsStrategy.Clamp };

        // Act
        var result = await _engine.CreateRecurrenceAsync(
            TestOrganization,
            TestResourcePath,
            TestType,
            startTime,
            TimeSpan.FromHours(1),
            rrule,
            TestTimeZone,
            options: options);

        // Assert
        Assert.Equal(MonthDayOutOfBoundsStrategy.Clamp, result.RecurrenceDetails?.MonthDayBehavior);
    }

    [Fact]
    public async Task GetOccurrencesAsync_Monthly31st_ClampStrategy_UsesLastDayOfMonth()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20240630T235959Z",
            new DateTime(2024, 6, 30, 23, 59, 59, DateTimeKind.Utc),
            monthDayBehavior: MonthDayOutOfBoundsStrategy.Clamp);

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 6, 30, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        // All 6 months should have one occurrence
        Assert.Equal(6, results.Count);

        // Check specific clamped days
        var febEntry = results.FirstOrDefault(r => r.StartTime.Month == 2);
        Assert.NotNull(febEntry);
        Assert.Equal(29, febEntry.StartTime.Day); // 2024 is a leap year

        var aprEntry = results.FirstOrDefault(r => r.StartTime.Month == 4);
        Assert.NotNull(aprEntry);
        Assert.Equal(30, aprEntry.StartTime.Day); // April has 30 days

        var junEntry = results.FirstOrDefault(r => r.StartTime.Month == 6);
        Assert.NotNull(junEntry);
        Assert.Equal(30, junEntry.StartTime.Day); // June has 30 days
    }

    [Fact]
    public async Task GetOccurrencesAsync_Monthly31st_ClampStrategy_ReturnsCorrectCount()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20241231T235959Z",
            new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            monthDayBehavior: MonthDayOutOfBoundsStrategy.Clamp);

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert - Every month should have exactly one occurrence
        Assert.Equal(12, results.Count);

        // Verify all months are represented
        var months = results.Select(r => r.StartTime.Month).OrderBy(m => m).ToList();
        Assert.Equal(Enumerable.Range(1, 12).ToList(), months);
    }

    [Fact]
    public async Task GetOccurrencesAsync_Monthly31st_ClampStrategy_NoDuplicates()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20241231T235959Z",
            new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            monthDayBehavior: MonthDayOutOfBoundsStrategy.Clamp);

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert - No duplicate dates
        var uniqueDates = results.Select(r => r.StartTime.Date).Distinct().Count();
        Assert.Equal(results.Count, uniqueDates);
    }

    [Fact]
    public async Task GetOccurrencesAsync_ClampStrategy_February_LeapYear_Returns29th()
    {
        // Arrange - 2024 is a leap year
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20240331T235959Z",
            new DateTime(2024, 3, 31, 23, 59, 59, DateTimeKind.Utc),
            monthDayBehavior: MonthDayOutOfBoundsStrategy.Clamp);

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 2, 29, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        Assert.Single(results);
        Assert.Equal(29, results[0].StartTime.Day);
    }

    [Fact]
    public async Task GetOccurrencesAsync_ClampStrategy_February_NonLeapYear_Returns28th()
    {
        // Arrange - 2025 is not a leap year
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2025, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20250331T235959Z",
            new DateTime(2025, 3, 31, 23, 59, 59, DateTimeKind.Utc),
            monthDayBehavior: MonthDayOutOfBoundsStrategy.Clamp);

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2025, 2, 28, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        Assert.Single(results);
        Assert.Equal(28, results[0].StartTime.Day);
    }

    [Fact]
    public async Task GetOccurrencesAsync_ClampStrategy_UntilBeforeClampedDay_NoOccurrence()
    {
        // Arrange - UNTIL is March 15, but BYMONTHDAY=30 would clamp to March 30
        // No March occurrence should be generated
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 30, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=30;UNTIL=20240315T235959Z",
            new DateTime(2024, 3, 15, 23, 59, 59, DateTimeKind.Utc),
            monthDayBehavior: MonthDayOutOfBoundsStrategy.Clamp);

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 3, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert - Only Jan 30 and Feb 29 (clamped from 30)
        // March 30 is after UNTIL=March 15, so no March occurrence
        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, r => r.StartTime.Month == 3);
    }

    [Fact]
    public async Task GetOccurrencesAsync_ClampStrategy_RespectsRecurrenceEndTime()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        // RecurrenceEndTime is Feb 15, but pattern goes to June
        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20240630T235959Z",
            new DateTime(2024, 2, 15, 23, 59, 59, DateTimeKind.Utc), // RecurrenceEndTime overrides
            monthDayBehavior: MonthDayOutOfBoundsStrategy.Clamp);

        SetupRecurrences([recurrence]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 6, 30, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert - Only Jan 31 because Feb 29 (clamped) > RecurrenceEndTime (Feb 15)
        Assert.Single(results);
        Assert.Equal(1, results[0].StartTime.Month);
    }

    #endregion

    #region Override and Exception on Clamped Occurrences Tests

    [Fact]
    public async Task GetOccurrencesAsync_ClampedOccurrence_WithException_ExcludesOccurrence()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20240430T235959Z",
            new DateTime(2024, 4, 30, 23, 59, 59, DateTimeKind.Utc),
            monthDayBehavior: MonthDayOutOfBoundsStrategy.Clamp);

        // Exception on the clamped April 30th occurrence (original would be April 31st but clamped to 30th)
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = new DateTime(2024, 4, 30, 9, 0, 0, DateTimeKind.Utc)
        };

        SetupRecurrences([recurrence]);
        SetupExceptions([exception]);

        var queryStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 4, 30, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert - Jan 31, Feb 29, Mar 31 = 3 occurrences (April 30 excepted)
        Assert.Equal(3, results.Count);
        Assert.DoesNotContain(results, r => r.StartTime.Month == 4);
    }

    [Fact]
    public async Task GetOccurrencesAsync_ClampedOccurrence_WithOverride_ReturnsOverriddenValues()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);

        var recurrence = CreateRecurrence(
            recurrenceId,
            startTime,
            duration,
            "FREQ=MONTHLY;BYMONTHDAY=31;UNTIL=20240430T235959Z",
            new DateTime(2024, 4, 30, 23, 59, 59, DateTimeKind.Utc),
            monthDayBehavior: MonthDayOutOfBoundsStrategy.Clamp);

        // Override on the clamped April 30th occurrence
        var overrideEntity = new OccurrenceOverride
        {
            Id = overrideId,
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = new DateTime(2024, 4, 30, 9, 0, 0, DateTimeKind.Utc),
            OriginalDuration = duration,
            Extensions = new Dictionary<string, string> { ["modified"] = "true" }
        };
        overrideEntity.Initialize(
            new DateTime(2024, 4, 30, 14, 0, 0, DateTimeKind.Utc), // Changed to 2 PM
            TimeSpan.FromHours(2)); // Extended to 2 hours

        SetupRecurrences([recurrence]);
        SetupOverrides([overrideEntity]);

        var queryStart = new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryEnd = new DateTime(2024, 4, 30, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var results = await GetResultsAsync(queryStart, queryEnd);

        // Assert
        Assert.Single(results);
        var entry = results[0];
        Assert.Equal(overrideId, entry.OverrideId);
        Assert.Equal(14, entry.StartTime.Hour);
        Assert.Equal(TimeSpan.FromHours(2), entry.Duration);
        Assert.True(entry.IsOverridden);
    }

    #endregion

    #region Exception Class Tests

    [Fact]
    public void MonthDayOutOfBoundsException_ContainsCorrectDayAndMonths()
    {
        // Arrange & Act
        var affectedMonths = new List<int> { 2, 4, 6, 9, 11 };
        var exception = new MonthDayOutOfBoundsException(31, affectedMonths);

        // Assert
        Assert.Equal(31, exception.DayOfMonth);
        Assert.Equal(affectedMonths, exception.AffectedMonths);
    }

    [Fact]
    public void MonthDayOutOfBoundsException_FormatsMonthNamesCorrectly()
    {
        // Arrange
        var affectedMonths = new List<int> { 2, 4 };

        // Act
        var exception = new MonthDayOutOfBoundsException(30, affectedMonths);

        // Assert - Message should contain readable month names
        Assert.Contains("February", exception.Message);
        Assert.Contains("April", exception.Message);
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

    private void SetupCreateRecurrence()
    {
        _recurrenceRepo
            .Setup(r => r.CreateAsync(
                It.IsAny<Recurrence>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recurrence r, ITransactionContext? _, CancellationToken _) => r);
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
        Dictionary<string, string>? extensions = null,
        MonthDayOutOfBoundsStrategy? monthDayBehavior = null)
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
            Extensions = extensions,
            MonthDayBehavior = monthDayBehavior
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
