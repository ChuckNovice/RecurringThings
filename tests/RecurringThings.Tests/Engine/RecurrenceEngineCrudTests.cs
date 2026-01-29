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
using Transactional.Abstractions;
using Xunit;

/// <summary>
/// Unit tests for RecurrenceEngine CRUD operations (Create, Update, Delete, Restore).
/// </summary>
public class RecurrenceEngineCrudTests
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
    private const string TestRRule = "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20251231T235959Z";

    public RecurrenceEngineCrudTests()
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

    #region CreateRecurrenceAsync Tests

    [Fact]
    public async Task CreateRecurrenceAsync_WithValidParameters_ReturnsRecurrenceWithGeneratedId()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);
        var extensions = new Dictionary<string, string> { ["key"] = "value" };

        _recurrenceRepo
            .Setup(r => r.CreateAsync(
                It.IsAny<Recurrence>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recurrence r, ITransactionContext? _, CancellationToken _) => r);

        // Act
        var result = await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            startTime, duration, TestRRule, TestTimeZone, extensions);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.RecurrenceId);
        Assert.Equal(CalendarEntryType.Recurrence, result.EntryType);
        Assert.Equal(TestOrganization, result.Organization);
        Assert.Equal(TestResourcePath, result.ResourcePath);
        Assert.Equal(TestType, result.Type);
        Assert.Equal(startTime, result.StartTime);
        Assert.Equal(duration, result.Duration);
        Assert.NotNull(result.RecurrenceDetails);
        Assert.Equal(TestRRule, result.RecurrenceDetails!.RRule);
        Assert.Equal(TestTimeZone, result.TimeZone);
        Assert.Equivalent(extensions, result.Extensions);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithNullOrganization_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _engine.CreateRecurrenceAsync(
            null!, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), TestRRule, TestTimeZone));
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithEmptyType_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, "",
            DateTime.UtcNow, TimeSpan.FromHours(1), TestRRule, TestTimeZone));

        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithRRuleContainingCount_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), "FREQ=DAILY;COUNT=10", TestTimeZone));

        Assert.Contains("COUNT", ex.Message);
        Assert.Contains("not supported", ex.Message);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithRRuleMissingUntil_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), "FREQ=DAILY;BYDAY=MO,TU,WE", TestTimeZone));

        Assert.Contains("UNTIL", ex.Message);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithNonUtcUntil_ThrowsArgumentException()
    {
        // Act - Missing Z suffix means non-UTC
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), "FREQ=DAILY;UNTIL=20251231T235959", TestTimeZone));

        Assert.Contains("UTC", ex.Message);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithInvalidTimeZone_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), TestRRule, "Invalid/TimeZone"));

        Assert.Equal("timeZone", ex.ParamName);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_PassesTransactionContextToRepository()
    {
        // Arrange
        var mockContext = new Mock<ITransactionContext>();

        _recurrenceRepo
            .Setup(r => r.CreateAsync(
                It.IsAny<Recurrence>(),
                mockContext.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recurrence r, ITransactionContext? _, CancellationToken _) => r);

        // Act
        await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), TestRRule, TestTimeZone,
            transactionContext: mockContext.Object);

        // Assert
        _recurrenceRepo.Verify(r => r.CreateAsync(
            It.IsAny<Recurrence>(),
            mockContext.Object,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateOccurrenceAsync Tests

    [Fact]
    public async Task CreateOccurrenceAsync_WithValidParameters_ReturnsOccurrenceWithComputedEndTime()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(30);
        var expectedEndTime = startTime + duration;

        _occurrenceRepo
            .Setup(r => r.CreateAsync(
                It.IsAny<Occurrence>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Occurrence o, ITransactionContext? _, CancellationToken _) => o);

        // Act
        var result = await _engine.CreateOccurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            startTime, duration, TestTimeZone);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.OccurrenceId);
        Assert.Equal(CalendarEntryType.Standalone, result.EntryType);
        Assert.Equal(TestOrganization, result.Organization);
        Assert.Equal(startTime, result.StartTime);
        Assert.Equal(duration, result.Duration);
        Assert.Equal(expectedEndTime, result.EndTime);
    }

    [Fact]
    public async Task CreateOccurrenceAsync_WithNullOrganization_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _engine.CreateOccurrenceAsync(
            null!, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromMinutes(30), TestTimeZone));
    }

    [Fact]
    public async Task CreateOccurrenceAsync_WithZeroDuration_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _engine.CreateOccurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.Zero, TestTimeZone));

        Assert.Equal("duration", ex.ParamName);
    }

    [Fact]
    public async Task CreateOccurrenceAsync_PassesTransactionContextToRepository()
    {
        // Arrange
        var mockContext = new Mock<ITransactionContext>();

        _occurrenceRepo
            .Setup(r => r.CreateAsync(
                It.IsAny<Occurrence>(),
                mockContext.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Occurrence o, ITransactionContext? _, CancellationToken _) => o);

        // Act
        await _engine.CreateOccurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromMinutes(30), TestTimeZone,
            transactionContext: mockContext.Object);

        // Assert
        _occurrenceRepo.Verify(r => r.CreateAsync(
            It.IsAny<Occurrence>(),
            mockContext.Object,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateOccurrenceAsync Tests - Recurrence (Blocked)

    [Fact]
    public async Task UpdateOccurrenceAsync_Recurrence_ThrowsInvalidOperationException()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId,
            EntryType = CalendarEntryType.Recurrence
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _engine.UpdateOccurrenceAsync(entry));

        Assert.Contains("Cannot update a recurrence pattern", ex.Message);
    }

    #endregion

    #region UpdateAsync Tests - Standalone Occurrence

    [Fact]
    public async Task UpdateAsync_StandaloneOccurrence_UpdatesStartTimeDurationExtensions()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var existingOccurrence = CreateOccurrence(occurrenceId);
        var newStartTime = existingOccurrence.StartTime.AddHours(1);
        var newDuration = TimeSpan.FromMinutes(45);

        _occurrenceRepo
            .Setup(r => r.GetByIdAsync(occurrenceId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync(existingOccurrence);

        _occurrenceRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Occurrence>(), null, default))
            .ReturnsAsync((Occurrence o, ITransactionContext? _, CancellationToken _) => o);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTime = newStartTime,
            Duration = newDuration,
            TimeZone = TestTimeZone,
            OccurrenceId = occurrenceId
        };

        // Act
        var result = await _engine.UpdateOccurrenceAsync(entry);

        // Assert
        Assert.Equal(newStartTime, result.StartTime);
        Assert.Equal(newDuration, result.Duration);
        Assert.Equal(newStartTime + newDuration, result.EndTime);
    }

    [Fact]
    public async Task UpdateAsync_StandaloneOccurrenceWithTypeChange_UpdatesTypeSuccessfully()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var existingOccurrence = CreateOccurrence(occurrenceId);
        var newType = "different-type";

        _occurrenceRepo
            .Setup(r => r.GetByIdAsync(occurrenceId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync(existingOccurrence);

        _occurrenceRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Occurrence>(), null, default))
            .ReturnsAsync((Occurrence o, ITransactionContext? _, CancellationToken _) => o);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = newType, // Changed - Type is mutable on standalone occurrences
            StartTime = existingOccurrence.StartTime,
            Duration = existingOccurrence.Duration,
            TimeZone = TestTimeZone,
            OccurrenceId = occurrenceId
        };

        // Act
        var result = await _engine.UpdateOccurrenceAsync(entry);

        // Assert
        Assert.Equal(newType, result.Type);
        _occurrenceRepo.Verify(r => r.UpdateAsync(
            It.Is<Occurrence>(o => o.Type == newType),
            null,
            default), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests - Virtualized Occurrence

    [Fact]
    public async Task UpdateAsync_VirtualizedOccurrenceWithoutOverride_CreatesOverride()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var recurrence = CreateRecurrence(recurrenceId);
        var originalStartTime = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var newDuration = TimeSpan.FromMinutes(45);

        _recurrenceRepo
            .Setup(r => r.GetByIdAsync(recurrenceId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync(recurrence);

        OccurrenceOverride? capturedOverride = null;
        _overrideRepo
            .Setup(r => r.CreateAsync(It.IsAny<OccurrenceOverride>(), null, default))
            .Callback<OccurrenceOverride, ITransactionContext?, CancellationToken>((o, _, _) => capturedOverride = o)
            .ReturnsAsync((OccurrenceOverride o, ITransactionContext? _, CancellationToken _) => o);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTime = originalStartTime,
            Duration = newDuration,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId,
            EntryType = CalendarEntryType.Virtualized,
            Original = new OriginalDetails
            {
                StartTime = originalStartTime,
                Duration = recurrence.Duration,
                Extensions = recurrence.Extensions
            }
        };

        // Act
        var result = await _engine.UpdateOccurrenceAsync(entry);

        // Assert
        Assert.NotNull(capturedOverride);
        Assert.Equal(recurrenceId, capturedOverride!.RecurrenceId);
        Assert.Equal(originalStartTime, capturedOverride.OriginalTimeUtc);
        Assert.Equal(recurrence.Duration, capturedOverride.OriginalDuration);
        Assert.Equal(newDuration, capturedOverride.Duration);
        Assert.NotNull(result.OverrideId);
    }

    [Fact]
    public async Task UpdateAsync_VirtualizedOccurrenceWithOverride_UpdatesExistingOverride()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();
        var recurrence = CreateRecurrence(recurrenceId);
        var existingOverride = CreateOverride(overrideId, recurrenceId);
        var newDuration = TimeSpan.FromMinutes(90);

        _recurrenceRepo
            .Setup(r => r.GetByIdAsync(recurrenceId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync(recurrence);

        _overrideRepo
            .Setup(r => r.GetByIdAsync(overrideId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync(existingOverride);

        _overrideRepo
            .Setup(r => r.UpdateAsync(It.IsAny<OccurrenceOverride>(), null, default))
            .ReturnsAsync((OccurrenceOverride o, ITransactionContext? _, CancellationToken _) => o);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTime = existingOverride.StartTime,
            Duration = newDuration,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId,
            OverrideId = overrideId,
            EntryType = CalendarEntryType.Virtualized,
            Original = new OriginalDetails
            {
                StartTime = existingOverride.OriginalTimeUtc,
                Duration = existingOverride.OriginalDuration,
                Extensions = existingOverride.OriginalExtensions
            }
        };

        // Act
        var result = await _engine.UpdateOccurrenceAsync(entry);

        // Assert
        _overrideRepo.Verify(r => r.UpdateAsync(
            It.Is<OccurrenceOverride>(o => o.Duration == newDuration),
            null, default), Times.Once);
        Assert.Equal(newDuration, result.Duration);
    }

    #endregion

    #region UpdateAsync Tests - Invalid Entry Types

    [Fact]
    public async Task UpdateAsync_WithNullEntry_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _engine.UpdateOccurrenceAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_WithIndeterminateEntryType_ThrowsInvalidOperationException()
    {
        // Arrange
        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone
            // No IDs set
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _engine.UpdateOccurrenceAsync(entry));

        Assert.Contains("Cannot determine entry type", ex.Message);
    }

    #endregion

    #region DeleteOccurrenceAsync Tests

    [Fact]
    public async Task DeleteOccurrenceAsync_Recurrence_ThrowsInvalidOperationException()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId,
            EntryType = CalendarEntryType.Recurrence
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _engine.DeleteOccurrenceAsync(entry));

        Assert.Contains("Use DeleteRecurrenceAsync", ex.Message);
    }

    [Fact]
    public async Task DeleteOccurrenceAsync_StandaloneOccurrence_CallsRepositoryDelete()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            OccurrenceId = occurrenceId
        };

        // Act
        await _engine.DeleteOccurrenceAsync(entry);

        // Assert
        _occurrenceRepo.Verify(r => r.DeleteAsync(
            occurrenceId, TestOrganization, TestResourcePath, null, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_VirtualizedOccurrenceWithoutOverride_CreatesException()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var originalTime = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);

        OccurrenceException? capturedExc = null;
        _exceptionRepo
            .Setup(r => r.CreateAsync(It.IsAny<OccurrenceException>(), null, default))
            .Callback<OccurrenceException, ITransactionContext?, CancellationToken>((e, _, _) => capturedExc = e)
            .ReturnsAsync((OccurrenceException e, ITransactionContext? _, CancellationToken _) => e);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTime = originalTime,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId,
            EntryType = CalendarEntryType.Virtualized,
            Original = new OriginalDetails
            {
                StartTime = originalTime,
                Duration = TimeSpan.FromHours(1),
                Extensions = null
            }
        };

        // Act
        await _engine.DeleteOccurrenceAsync(entry);

        // Assert
        Assert.NotNull(capturedExc);
        Assert.Equal(recurrenceId, capturedExc!.RecurrenceId);
        Assert.Equal(originalTime, capturedExc.OriginalTimeUtc);
    }

    [Fact]
    public async Task DeleteAsync_VirtualizedOccurrenceWithOverride_DeletesOverrideAndCreatesException()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();
        var originalTime = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);

        OccurrenceException? capturedExc = null;
        _exceptionRepo
            .Setup(r => r.CreateAsync(It.IsAny<OccurrenceException>(), null, default))
            .Callback<OccurrenceException, ITransactionContext?, CancellationToken>((e, _, _) => capturedExc = e)
            .ReturnsAsync((OccurrenceException e, ITransactionContext? _, CancellationToken _) => e);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTime = originalTime.AddHours(1), // Moved time
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId,
            OverrideId = overrideId,
            EntryType = CalendarEntryType.Virtualized,
            Original = new OriginalDetails
            {
                StartTime = originalTime, // Original time
                Duration = TimeSpan.FromHours(1),
                Extensions = null
            }
        };

        // Act
        await _engine.DeleteOccurrenceAsync(entry);

        // Assert
        _overrideRepo.Verify(r => r.DeleteAsync(
            overrideId, TestOrganization, TestResourcePath, null, default), Times.Once);
        Assert.NotNull(capturedExc);
        Assert.Equal(originalTime, capturedExc!.OriginalTimeUtc); // Exception at original time, not moved time
    }

    [Fact]
    public async Task DeleteAsync_WithNullEntry_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _engine.DeleteOccurrenceAsync(null!));
    }

    #endregion

    #region RestoreAsync Tests

    [Fact]
    public async Task RestoreAsync_OverriddenOccurrence_DeletesOverride()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();
        var originalTime = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId,
            OverrideId = overrideId,
            EntryType = CalendarEntryType.Virtualized,
            Original = new OriginalDetails
            {
                StartTime = originalTime,
                Duration = TimeSpan.FromHours(1),
                Extensions = null
            }
        };

        // Act
        await _engine.RestoreAsync(entry);

        // Assert
        _overrideRepo.Verify(r => r.DeleteAsync(
            overrideId, TestOrganization, TestResourcePath, null, default), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_Recurrence_ThrowsInvalidOperationException()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId,
            EntryType = CalendarEntryType.Recurrence
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _engine.RestoreAsync(entry));

        Assert.Contains("recurrence pattern", ex.Message);
    }

    [Fact]
    public async Task RestoreAsync_StandaloneOccurrence_ThrowsInvalidOperationException()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            OccurrenceId = occurrenceId,
            EntryType = CalendarEntryType.Standalone
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _engine.RestoreAsync(entry));

        Assert.Contains("standalone occurrence", ex.Message);
    }

    [Fact]
    public async Task RestoreAsync_VirtualizedOccurrenceWithoutOverride_ThrowsInvalidOperationException()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId,
            // No OverrideId
            EntryType = CalendarEntryType.Virtualized,
            Original = new OriginalDetails
            {
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.FromHours(1),
                Extensions = null
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _engine.RestoreAsync(entry));

        Assert.Contains("without an override", ex.Message);
    }

    [Fact]
    public async Task RestoreAsync_WithNullEntry_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _engine.RestoreAsync(null!));
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
            StartTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            RRule = "FREQ=DAILY;UNTIL=20251231T235959Z",
            TimeZone = TestTimeZone,
            Extensions = new Dictionary<string, string> { ["key"] = "value" }
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
            Extensions = new Dictionary<string, string> { ["key"] = "value" }
        };
        occ.Initialize(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(30));
        return occ;
    }

    private static OccurrenceOverride CreateOverride(Guid id, Guid recurrenceId)
    {
        var @override = new OccurrenceOverride
        {
            Id = id,
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc),
            OriginalDuration = TimeSpan.FromHours(1),
            OriginalExtensions = new Dictionary<string, string> { ["key"] = "value" },
            Extensions = new Dictionary<string, string> { ["modified"] = "true" }
        };
        @override.Initialize(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(45));
        return @override;
    }

    #endregion
}
