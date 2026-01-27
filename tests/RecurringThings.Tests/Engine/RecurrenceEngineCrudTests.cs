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
    private const string TestTimeZone = "America/New_York";

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
    public async Task CreateRecurrenceAsync_WithValidRequest_ReturnsRecurrenceWithGeneratedId()
    {
        // Arrange
        var request = CreateValidRecurrenceRequest();
        Recurrence? capturedRecurrence = null;

        _recurrenceRepo
            .Setup(r => r.CreateAsync(
                It.IsAny<Recurrence>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Recurrence, ITransactionContext?, CancellationToken>((r, _, _) => capturedRecurrence = r)
            .ReturnsAsync((Recurrence r, ITransactionContext? _, CancellationToken _) => r);

        // Act
        var result = await _engine.CreateRecurrenceAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Organization.Should().Be(request.Organization);
        result.ResourcePath.Should().Be(request.ResourcePath);
        result.Type.Should().Be(request.Type);
        result.StartTime.Should().Be(request.StartTimeUtc);
        result.Duration.Should().Be(request.Duration);
        result.RecurrenceEndTime.Should().Be(request.RecurrenceEndTimeUtc);
        result.RRule.Should().Be(request.RRule);
        result.TimeZone.Should().Be(request.TimeZone);
        result.Extensions.Should().BeEquivalentTo(request.Extensions);
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _engine.CreateRecurrenceAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithEmptyType_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRecurrenceRequest(type: "");

        // Act
        var act = () => _engine.CreateRecurrenceAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("type");
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithRRuleContainingCount_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRecurrenceRequest(rrule: "FREQ=DAILY;COUNT=10");

        // Act
        var act = () => _engine.CreateRecurrenceAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*COUNT*not supported*");
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithRRuleMissingUntil_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRecurrenceRequest(rrule: "FREQ=DAILY;BYDAY=MO,TU,WE");

        // Act
        var act = () => _engine.CreateRecurrenceAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*UNTIL*");
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithNonUtcUntil_ThrowsArgumentException()
    {
        // Arrange - Missing Z suffix means non-UTC
        var request = CreateValidRecurrenceRequest(rrule: "FREQ=DAILY;UNTIL=20251231T235959");

        // Act
        var act = () => _engine.CreateRecurrenceAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*UTC*");
    }

    [Fact]
    public async Task CreateRecurrenceAsync_WithInvalidTimeZone_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRecurrenceRequest(timeZone: "Invalid/TimeZone");

        // Act
        var act = () => _engine.CreateRecurrenceAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("timeZone");
    }

    [Fact]
    public async Task CreateRecurrenceAsync_PassesTransactionContextToRepository()
    {
        // Arrange
        var request = CreateValidRecurrenceRequest();
        var mockContext = new Mock<ITransactionContext>();

        _recurrenceRepo
            .Setup(r => r.CreateAsync(
                It.IsAny<Recurrence>(),
                mockContext.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recurrence r, ITransactionContext? _, CancellationToken _) => r);

        // Act
        await _engine.CreateRecurrenceAsync(request, mockContext.Object);

        // Assert
        _recurrenceRepo.Verify(r => r.CreateAsync(
            It.IsAny<Recurrence>(),
            mockContext.Object,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateOccurrenceAsync Tests

    [Fact]
    public async Task CreateOccurrenceAsync_WithValidRequest_ReturnsOccurrenceWithComputedEndTime()
    {
        // Arrange
        var request = CreateValidOccurrenceRequest();
        var expectedEndTime = request.StartTimeUtc + request.Duration;

        _occurrenceRepo
            .Setup(r => r.CreateAsync(
                It.IsAny<Occurrence>(),
                It.IsAny<ITransactionContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Occurrence o, ITransactionContext? _, CancellationToken _) => o);

        // Act
        var result = await _engine.CreateOccurrenceAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Organization.Should().Be(request.Organization);
        result.StartTime.Should().Be(request.StartTimeUtc);
        result.Duration.Should().Be(request.Duration);
        result.EndTime.Should().Be(expectedEndTime);
    }

    [Fact]
    public async Task CreateOccurrenceAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _engine.CreateOccurrenceAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateOccurrenceAsync_WithZeroDuration_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidOccurrenceRequest(duration: TimeSpan.Zero);

        // Act
        var act = () => _engine.CreateOccurrenceAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("duration");
    }

    [Fact]
    public async Task CreateOccurrenceAsync_PassesTransactionContextToRepository()
    {
        // Arrange
        var request = CreateValidOccurrenceRequest();
        var mockContext = new Mock<ITransactionContext>();

        _occurrenceRepo
            .Setup(r => r.CreateAsync(
                It.IsAny<Occurrence>(),
                mockContext.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Occurrence o, ITransactionContext? _, CancellationToken _) => o);

        // Act
        await _engine.CreateOccurrenceAsync(request, mockContext.Object);

        // Assert
        _occurrenceRepo.Verify(r => r.CreateAsync(
            It.IsAny<Occurrence>(),
            mockContext.Object,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests - Recurrence

    [Fact]
    public async Task UpdateAsync_RecurrenceDuration_UpdatesSuccessfully()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var existingRecurrence = CreateRecurrence(recurrenceId);
        var newDuration = TimeSpan.FromHours(2);

        _recurrenceRepo
            .Setup(r => r.GetByIdAsync(recurrenceId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync(existingRecurrence);

        _recurrenceRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Recurrence>(), null, default))
            .ReturnsAsync((Recurrence r, ITransactionContext? _, CancellationToken _) => r);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTime = existingRecurrence.StartTime,
            Duration = newDuration,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId
        };

        // Act
        var result = await _engine.UpdateAsync(entry);

        // Assert
        result.Duration.Should().Be(newDuration);
        _recurrenceRepo.Verify(r => r.UpdateAsync(
            It.Is<Recurrence>(rec => rec.Duration == newDuration),
            null, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RecurrenceExtensions_UpdatesSuccessfully()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var existingRecurrence = CreateRecurrence(recurrenceId);
        var newExtensions = new Dictionary<string, string> { ["newKey"] = "newValue" };

        _recurrenceRepo
            .Setup(r => r.GetByIdAsync(recurrenceId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync(existingRecurrence);

        _recurrenceRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Recurrence>(), null, default))
            .ReturnsAsync((Recurrence r, ITransactionContext? _, CancellationToken _) => r);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTime = existingRecurrence.StartTime,
            Duration = existingRecurrence.Duration,
            TimeZone = TestTimeZone,
            Extensions = newExtensions,
            RecurrenceId = recurrenceId
        };

        // Act
        var result = await _engine.UpdateAsync(entry);

        // Assert
        result.Extensions.Should().BeEquivalentTo(newExtensions);
    }

    [Fact]
    public async Task UpdateAsync_RecurrenceWithImmutableTypeChange_ThrowsInvalidOperationException()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var existingRecurrence = CreateRecurrence(recurrenceId);

        _recurrenceRepo
            .Setup(r => r.GetByIdAsync(recurrenceId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync(existingRecurrence);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = "different-type", // Changed!
            StartTime = existingRecurrence.StartTime,
            Duration = existingRecurrence.Duration,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId
        };

        // Act
        var act = () => _engine.UpdateAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Type*immutable*");
    }

    [Fact]
    public async Task UpdateAsync_RecurrenceWithImmutableStartTimeChange_ThrowsInvalidOperationException()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var existingRecurrence = CreateRecurrence(recurrenceId);

        _recurrenceRepo
            .Setup(r => r.GetByIdAsync(recurrenceId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync(existingRecurrence);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTime = existingRecurrence.StartTime.AddHours(1), // Changed!
            Duration = existingRecurrence.Duration,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId
        };

        // Act
        var act = () => _engine.UpdateAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*StartTime*immutable*");
    }

    [Fact]
    public async Task UpdateAsync_RecurrenceNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();

        _recurrenceRepo
            .Setup(r => r.GetByIdAsync(recurrenceId, TestOrganization, TestResourcePath, null, default))
            .ReturnsAsync((Recurrence?)null);

        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId
        };

        // Act
        var act = () => _engine.UpdateAsync(entry);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
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
        var result = await _engine.UpdateAsync(entry);

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
        var act = () => _engine.UpdateAsync(entry);

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
            RecurrenceOccurrenceDetails = new RecurrenceOccurrenceDetails
            {
                RecurrenceId = recurrenceId,
                Original = new OccurrenceOriginal
                {
                    StartTime = originalStartTime,
                    Duration = recurrence.Duration,
                    Extensions = recurrence.Extensions
                }
            }
        };

        // Act
        var result = await _engine.UpdateAsync(entry);

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
            RecurrenceOccurrenceDetails = new RecurrenceOccurrenceDetails
            {
                RecurrenceId = recurrenceId,
                Original = new OccurrenceOriginal
                {
                    StartTime = existingOverride.OriginalTimeUtc,
                    Duration = existingOverride.OriginalDuration,
                    Extensions = existingOverride.OriginalExtensions
                }
            }
        };

        // Act
        var result = await _engine.UpdateAsync(entry);

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
        var act = () => _engine.UpdateAsync(null!);

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
        var act = () => _engine.UpdateAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot determine entry type*");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_Recurrence_CallsRepositoryDelete()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var entry = new CalendarEntry
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            TimeZone = TestTimeZone,
            RecurrenceId = recurrenceId
        };

        // Act
        await _engine.DeleteAsync(entry);

        // Assert
        _recurrenceRepo.Verify(r => r.DeleteAsync(
            recurrenceId, TestOrganization, TestResourcePath, null, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_StandaloneOccurrence_CallsRepositoryDelete()
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
        await _engine.DeleteAsync(entry);

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
            RecurrenceOccurrenceDetails = new RecurrenceOccurrenceDetails
            {
                RecurrenceId = recurrenceId,
                Original = new OccurrenceOriginal
                {
                    StartTime = originalTime,
                    Duration = TimeSpan.FromHours(1),
                    Extensions = null
                }
            }
        };

        // Act
        await _engine.DeleteAsync(entry);

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
            RecurrenceOccurrenceDetails = new RecurrenceOccurrenceDetails
            {
                RecurrenceId = recurrenceId,
                Original = new OccurrenceOriginal
                {
                    StartTime = originalTime, // Original time
                    Duration = TimeSpan.FromHours(1),
                    Extensions = null
                }
            }
        };

        // Act
        await _engine.DeleteAsync(entry);

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
        var act = () => _engine.DeleteAsync(null!);

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
            RecurrenceOccurrenceDetails = new RecurrenceOccurrenceDetails
            {
                RecurrenceId = recurrenceId,
                Original = new OccurrenceOriginal
                {
                    StartTime = originalTime,
                    Duration = TimeSpan.FromHours(1),
                    Extensions = null
                }
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
            RecurrenceId = recurrenceId
            // No RecurrenceOccurrenceDetails = it's a recurrence pattern
        };

        // Act
        var act = () => _engine.RestoreAsync(entry);

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
            OccurrenceId = occurrenceId
        };

        // Act
        var act = () => _engine.RestoreAsync(entry);

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
            RecurrenceOccurrenceDetails = new RecurrenceOccurrenceDetails
            {
                RecurrenceId = recurrenceId,
                Original = new OccurrenceOriginal
                {
                    StartTime = DateTime.UtcNow,
                    Duration = TimeSpan.FromHours(1),
                    Extensions = null
                }
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

    private static RecurrenceCreate CreateValidRecurrenceRequest(
        string? type = null,
        string? rrule = null,
        string? timeZone = null)
    {
        return new RecurrenceCreate
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = type ?? TestType,
            StartTimeUtc = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTimeUtc = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            RRule = rrule ?? "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20251231T235959Z",
            TimeZone = timeZone ?? TestTimeZone,
            Extensions = new Dictionary<string, string> { ["key"] = "value" }
        };
    }

    private static OccurrenceCreate CreateValidOccurrenceRequest(TimeSpan? duration = null)
    {
        return new OccurrenceCreate
        {
            Organization = TestOrganization,
            ResourcePath = TestResourcePath,
            Type = TestType,
            StartTimeUtc = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            Duration = duration ?? TimeSpan.FromMinutes(30),
            TimeZone = TestTimeZone,
            Extensions = new Dictionary<string, string> { ["meeting"] = "standup" }
        };
    }

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
