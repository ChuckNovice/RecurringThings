namespace RecurringThings.Tests.Engine;

using System;
using System.Collections.Generic;
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
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Organization.Should().Be(TestOrganization);
        result.ResourcePath.Should().Be(TestResourcePath);
        result.Type.Should().Be(TestType);
        result.StartTime.Should().Be(startTime);
        result.Duration.Should().Be(duration);
        // RecurrenceEndTime is extracted from RRule UNTIL clause
        result.RecurrenceEndTime.Should().Be(new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc));
        result.RRule.Should().Be(TestRRule);
        result.TimeZone.Should().Be(TestTimeZone);
        result.Extensions.Should().BeEquivalentTo(extensions);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithNullOrganization_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _engine.CreateRecurrenceAsync(
            null!, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), TestRRule, TestTimeZone);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithEmptyType_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, "",
            DateTime.UtcNow, TimeSpan.FromHours(1), TestRRule, TestTimeZone);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("type");
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithRRuleContainingCount_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), "FREQ=DAILY;COUNT=10", TestTimeZone);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*COUNT*not supported*");
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithRRuleMissingUntil_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), "FREQ=DAILY;BYDAY=MO,TU,WE", TestTimeZone);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*UNTIL*");
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithNonUtcUntil_ThrowsArgumentException()
    {
        // Act - Missing Z suffix means non-UTC
        var act = async () => await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), "FREQ=DAILY;UNTIL=20251231T235959", TestTimeZone);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*UTC*");
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithInvalidTimeZone_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _engine.CreateRecurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromHours(1), TestRRule, "Invalid/TimeZone");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("timeZone");
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
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Organization.Should().Be(TestOrganization);
        result.StartTime.Should().Be(startTime);
        result.Duration.Should().Be(duration);
        result.EndTime.Should().Be(expectedEndTime);
    }

    [Fact]
    public async Task CreateOccurrenceAsync_WithNullOrganization_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _engine.CreateOccurrenceAsync(
            null!, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.FromMinutes(30), TestTimeZone);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateOccurrenceAsync_WithZeroDuration_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _engine.CreateOccurrenceAsync(
            TestOrganization, TestResourcePath, TestType,
            DateTime.UtcNow, TimeSpan.Zero, TestTimeZone);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("duration");
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

        // Act
        var act = async () => await _engine.UpdateOccurrenceAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot update a recurrence pattern*");
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
        result.StartTime.Should().Be(newStartTime);
        result.Duration.Should().Be(newDuration);
        result.EndTime.Should().Be(newStartTime + newDuration);
    }

    [Fact]
    public async Task UpdateAsync_StandaloneOccurrenceWithImmutableTypeChange_ThrowsInvalidOperationException()
    {
        // Arrange
        var occurrenceId = Guid.NewGuid();
        var existingOccurrence = CreateOccurrence(occurrenceId);

        _occurrenceRepo
            .Setup(r => r.GetByIdAsync(occurrenceId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync(existingOccurrence);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = "different-type", // Changed!
            StartTime = existingOccurrence.StartTime,
            Duration = existingOccurrence.Duration,
            TimeZone = TestTimeZone,
            OccurrenceId = occurrenceId
        };

        // Act
        var act = () => _engine.UpdateOccurrenceAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Type*immutable*");
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
        capturedOverride.Should().NotBeNull();
        capturedOverride!.RecurrenceId.Should().Be(recurrenceId);
        capturedOverride.OriginalTimeUtc.Should().Be(originalStartTime);
        capturedOverride.OriginalDuration.Should().Be(recurrence.Duration);
        capturedOverride.Duration.Should().Be(newDuration);
        result.OverrideId.Should().NotBeNull();
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
        result.Duration.Should().Be(newDuration);
    }

    #endregion

    #region UpdateAsync Tests - Invalid Entry Types

    [Fact]
    public async Task UpdateAsync_WithNullEntry_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _engine.UpdateOccurrenceAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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

        // Act
        var act = () => _engine.UpdateOccurrenceAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot determine entry type*");
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

        // Act
        var act = async () => await _engine.DeleteOccurrenceAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Use DeleteRecurrenceAsync*");
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
        capturedExc.Should().NotBeNull();
        capturedExc!.RecurrenceId.Should().Be(recurrenceId);
        capturedExc.OriginalTimeUtc.Should().Be(originalTime);
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
        capturedExc.Should().NotBeNull();
        capturedExc!.OriginalTimeUtc.Should().Be(originalTime); // Exception at original time, not moved time
    }

    [Fact]
    public async Task DeleteAsync_WithNullEntry_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _engine.DeleteOccurrenceAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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

        // Act
        var act = async () => await _engine.RestoreAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*recurrence pattern*");
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

        // Act
        var act = async () => await _engine.RestoreAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*standalone occurrence*");
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

        // Act
        var act = () => _engine.RestoreAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*without an override*");
    }

    [Fact]
    public async Task RestoreAsync_WithNullEntry_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _engine.RestoreAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
