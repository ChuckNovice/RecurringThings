namespace RecurringThings.Tests.Engine;

using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Moq;
using RecurringThings.Engine;
using RecurringThings.Repository;

/// <summary>
/// Unit tests for <see cref="RecurrenceEngine.CreateEventAsync"/>.
/// </summary>
public sealed class RecurrenceEngineCreateEventAsyncTests
{
    private readonly Mock<IRecurringThingsRepository> _repositoryMock;
    private readonly RecurrenceEngine _engine;

    public RecurrenceEngineCreateEventAsyncTests()
    {
        _repositoryMock = new Mock<IRecurringThingsRepository>();
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _engine = new RecurrenceEngine(_repositoryMock.Object);
    }

    #region Parameter Validation Tests

    /// <summary>
    /// Tests that CreateEventAsync throws ArgumentNullException when entry is null.
    /// </summary>
    [Fact]
    public async Task GivenNullEntry_WhenCreateEventAsync_ThenThrowsArgumentNullException()
    {
        // Arrange
        CalendarEvent entry = null!;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _engine.CreateEventAsync(entry, "tenant1"));
        Assert.Equal("entry", exception.ParamName);
    }

    /// <summary>
    /// Tests that CreateEventAsync throws ArgumentNullException when entry.Uid is null.
    /// </summary>
    [Fact]
    public async Task GivenNullUid_WhenCreateEventAsync_ThenThrowsArgumentNullException()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = null };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _engine.CreateEventAsync(entry, "tenant1"));
    }

    /// <summary>
    /// Tests that CreateEventAsync throws ArgumentException when entry.Uid is empty.
    /// </summary>
    [Fact]
    public async Task GivenEmptyUid_WhenCreateEventAsync_ThenThrowsArgumentException()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _engine.CreateEventAsync(entry, "tenant1"));
    }

    /// <summary>
    /// Tests that CreateEventAsync throws ArgumentException when entry.Uid is whitespace.
    /// </summary>
    [Fact]
    public async Task GivenWhitespaceUid_WhenCreateEventAsync_ThenThrowsArgumentException()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "   " };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _engine.CreateEventAsync(entry, "tenant1"));
    }

    /// <summary>
    /// Tests that CreateEventAsync throws ArgumentException when entry has duplicate categories (case-insensitive).
    /// </summary>
    [Fact]
    public async Task GivenDuplicateCategories_WhenCreateEventAsync_ThenThrowsArgumentException()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };
        entry.Categories.Add("Work");
        entry.Categories.Add("WORK"); // Duplicate (case-insensitive)

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _engine.CreateEventAsync(entry, "tenant1"));
        Assert.Contains("Categories", exception.Message);
        Assert.Equal("entry", exception.ParamName);
    }

    /// <summary>
    /// Tests that CreateEventAsync throws ArgumentException when entry has duplicate property names (case-insensitive).
    /// </summary>
    [Fact]
    public async Task GivenDuplicatePropertyNames_WhenCreateEventAsync_ThenThrowsArgumentException()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };
        entry.Properties.Add(new CalendarProperty("X-CUSTOM", "value1"));
        entry.Properties.Add(new CalendarProperty("x-custom", "value2")); // Duplicate (case-insensitive)

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _engine.CreateEventAsync(entry, "tenant1"));
        Assert.Contains("Properties", exception.Message);
        Assert.Contains("unique names", exception.Message);
        Assert.Equal("entry", exception.ParamName);
    }

    /// <summary>
    /// Tests that adding a property with null name throws at Ical.Net level.
    /// Note: Ical.Net throws ArgumentNullException when adding a property with null name,
    /// so our engine validation is never reached for this case.
    /// </summary>
    [Fact]
    public void GivenNullPropertyName_WhenAddingToCalendarEvent_ThenIcalNetThrowsArgumentNullException()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };

        // Act & Assert - Ical.Net throws when adding a property with null name
        Assert.Throws<ArgumentNullException>(
            () => entry.Properties.Add(new CalendarProperty(null!, "value")));
    }

    /// <summary>
    /// Tests that CreateEventAsync throws ArgumentException when property name is whitespace.
    /// </summary>
    [Fact]
    public async Task GivenWhitespacePropertyName_WhenCreateEventAsync_ThenThrowsArgumentException()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };
        entry.Properties.Add(new CalendarProperty("   ", "value"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _engine.CreateEventAsync(entry, "tenant1"));
        Assert.Contains("Properties", exception.Message);
        Assert.Contains("null or whitespace names", exception.Message);
        Assert.Equal("entry", exception.ParamName);
    }

    #endregion

    #region Component Type Tests

    /// <summary>
    /// Tests that CreateEventAsync correctly identifies CalendarEvent as ComponentType.Event.
    /// </summary>
    [Fact]
    public async Task GivenCalendarEvent_WhenCreateEventAsync_ThenPassesEventComponentType()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "event-uid" };

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<EventMetadata>(m => m.Uid == "event-uid" && m.ComponentType == ComponentType.Event),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that CreateEventAsync correctly identifies Todo as ComponentType.Todo.
    /// </summary>
    [Fact]
    public async Task GivenTodo_WhenCreateEventAsync_ThenPassesTodoComponentType()
    {
        // Arrange
        var entry = new Todo { Uid = "todo-uid" };

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<EventMetadata>(m => m.Uid == "todo-uid" && m.ComponentType == ComponentType.Todo),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that CreateEventAsync correctly identifies Journal as ComponentType.Journal.
    /// </summary>
    [Fact]
    public async Task GivenJournal_WhenCreateEventAsync_ThenPassesJournalComponentType()
    {
        // Arrange
        var entry = new Journal { Uid = "journal-uid" };

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<EventMetadata>(m => m.Uid == "journal-uid" && m.ComponentType == ComponentType.Journal),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Repository Call Verification Tests

    /// <summary>
    /// Tests that CreateEventAsync passes tenantId and userId correctly to repository.
    /// </summary>
    [Fact]
    public async Task GivenTenantAndUser_WhenCreateEventAsync_ThenPassesThemToRepository()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };

        // Act
        await _engine.CreateEventAsync(entry, "my-tenant", "user-123");

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<EventMetadata>(m => m.TenantId == "my-tenant" && m.UserId == "user-123"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that CreateEventAsync passes null userId when not provided.
    /// </summary>
    [Fact]
    public async Task GivenNoUserId_WhenCreateEventAsync_ThenPassesNullUserId()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<EventMetadata>(m => m.TenantId == "tenant1" && m.UserId == null),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that CreateEventAsync correctly extracts start date as UTC.
    /// CalDateTime with UTC timezone returns the time as-is in UTC.
    /// </summary>
    [Fact]
    public async Task GivenUtcStartDate_WhenCreateEventAsync_ThenReturnsUtc()
    {
        // Arrange
        // Use explicit UTC timezone for CalDateTime
        var utcStart = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(utcStart, "UTC")
        };

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.NotNull(capturedMetadata.StartDate);
        Assert.Equal(DateTimeKind.Utc, capturedMetadata.StartDate.Value.Kind);
        Assert.Equal(utcStart, capturedMetadata.StartDate.Value);
    }

    /// <summary>
    /// Tests that CreateEventAsync lowercases categories.
    /// </summary>
    [Fact]
    public async Task GivenMixedCaseCategories_WhenCreateEventAsync_ThenLowercasesCategories()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };
        entry.Categories.Add("WORK");
        entry.Categories.Add("Personal");
        entry.Categories.Add("urgent");

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.Equal(3, capturedMetadata.Categories.Count);
        Assert.Contains("work", capturedMetadata.Categories);
        Assert.Contains("personal", capturedMetadata.Categories);
        Assert.Contains("urgent", capturedMetadata.Categories);
    }

    /// <summary>
    /// Tests that CreateEventAsync correctly extracts property name and value.
    /// Note: Ical.Net adds default properties (UID, DTSTAMP, SEQUENCE) automatically.
    /// </summary>
    [Fact]
    public async Task GivenPropertiesWithValues_WhenCreateEventAsync_ThenExtractsNameAndValue()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };
        entry.Properties.Add(new CalendarProperty("X-CUSTOM-PROP", "custom-value"));
        entry.Properties.Add(new CalendarProperty("X-ANOTHER", "another-value"));

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert - Ical.Net adds default properties (UID, DTSTAMP, SEQUENCE), so check our custom ones are present
        // Note: Property names are lowercased for case-insensitive matching
        Assert.NotNull(capturedMetadata);
        Assert.True(capturedMetadata.Properties.Count >= 2, "Should have at least the 2 custom properties");
        Assert.True(capturedMetadata.Properties.ContainsKey("x-custom-prop"));
        Assert.Equal("custom-value", capturedMetadata.Properties["x-custom-prop"]);
        Assert.True(capturedMetadata.Properties.ContainsKey("x-another"));
        Assert.Equal("another-value", capturedMetadata.Properties["x-another"]);
    }

    /// <summary>
    /// Tests that CreateEventAsync handles null property value by preserving null.
    /// Note: Ical.Net adds default properties (UID, DTSTAMP, SEQUENCE) automatically.
    /// </summary>
    [Fact]
    public async Task GivenPropertyWithNullValue_WhenCreateEventAsync_ThenPreservesNull()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };
        entry.Properties.Add(new CalendarProperty("X-NULL-VALUE", null));

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert - Find our custom property (Ical.Net adds default properties)
        Assert.NotNull(capturedMetadata);
        Assert.True(capturedMetadata.Properties.ContainsKey("x-null-value")); // Keys are lowercased
        Assert.Null(capturedMetadata.Properties["x-null-value"]);
    }

    /// <summary>
    /// Tests that CreateEventAsync passes serialized iCalendar data to repository.
    /// </summary>
    [Fact]
    public async Task GivenEvent_WhenCreateEventAsync_ThenPassesSerializedICalendarData()
    {
        // Arrange
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Summary = "Test Event"
        };

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.Contains("BEGIN:VCALENDAR", capturedMetadata.SerializedData);
        Assert.Contains("BEGIN:VEVENT", capturedMetadata.SerializedData);
        Assert.Contains("UID:test-uid", capturedMetadata.SerializedData);
        Assert.Contains("SUMMARY:Test Event", capturedMetadata.SerializedData);
        Assert.Contains("END:VEVENT", capturedMetadata.SerializedData);
        Assert.Contains("END:VCALENDAR", capturedMetadata.SerializedData);
    }

    /// <summary>
    /// Tests that CreateEventAsync passes CancellationToken to repository.
    /// </summary>
    [Fact]
    public async Task GivenCancellationToken_WhenCreateEventAsync_ThenPassesToRepository()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _engine.CreateEventAsync(entry, "tenant1", cancellationToken: token);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.IsAny<EventMetadata>(),
            token),
            Times.Once);
    }

    #endregion

    #region GetEndDate Tests - Single Events (No Recurrence)

    /// <summary>
    /// Tests that GetEndDate returns the event's end time for a single event with explicit end.
    /// </summary>
    [Fact]
    public async Task GivenSingleEventWithEnd_WhenCreateEventAsync_ThenEndDateIsEventEnd()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var endUtc = new DateTime(2025, 6, 15, 11, 0, 0, DateTimeKind.Utc);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(endUtc)
        };

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.NotNull(capturedMetadata.EndDate);
        Assert.Equal(DateTimeKind.Utc, capturedMetadata.EndDate.Value.Kind);
        Assert.Equal(endUtc, capturedMetadata.EndDate.Value);
    }

    /// <summary>
    /// Tests that GetEndDate returns the event's computed end time for a single event with duration.
    /// </summary>
    [Fact]
    public async Task GivenSingleEventWithDuration_WhenCreateEventAsync_ThenEndDateIsStartPlusDuration()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var endUtc = startUtc.AddHours(2);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(endUtc)
        };

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.NotNull(capturedMetadata.EndDate);
        Assert.Equal(DateTimeKind.Utc, capturedMetadata.EndDate.Value.Kind);
        Assert.Equal(endUtc, capturedMetadata.EndDate.Value);
    }

    /// <summary>
    /// Tests that GetEndDate returns null for an event with no start date.
    /// </summary>
    [Fact]
    public async Task GivenEventWithNoStartDate_WhenCreateEventAsync_ThenEndDateIsNull()
    {
        // Arrange
        var entry = new CalendarEvent { Uid = "test-uid" };
        // No Start set

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.Null(capturedMetadata.EndDate);
    }

    #endregion

    #region GetEndDate Tests - Infinite Recurrence

    /// <summary>
    /// Tests that GetEndDate returns null for an infinite daily recurrence (no UNTIL, no COUNT).
    /// </summary>
    [Fact]
    public async Task GivenInfiniteDailyRecurrence_WhenCreateEventAsync_ThenEndDateIsNull()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(startUtc.AddHours(1))
        };
        entry.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1)); // No UNTIL, no COUNT

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.Null(capturedMetadata.EndDate);
    }

    /// <summary>
    /// Tests that GetEndDate returns null for an infinite weekly recurrence.
    /// </summary>
    [Fact]
    public async Task GivenInfiniteWeeklyRecurrence_WhenCreateEventAsync_ThenEndDateIsNull()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(startUtc.AddHours(1))
        };
        entry.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Weekly, 1)); // No UNTIL, no COUNT

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.Null(capturedMetadata.EndDate);
    }

    /// <summary>
    /// Tests that GetEndDate returns null for an infinite monthly recurrence.
    /// </summary>
    [Fact]
    public async Task GivenInfiniteMonthlyRecurrence_WhenCreateEventAsync_ThenEndDateIsNull()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(startUtc.AddHours(1))
        };
        entry.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Monthly, 1)); // No UNTIL, no COUNT

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.Null(capturedMetadata.EndDate);
    }

    #endregion

    #region GetEndDate Tests - Finite Recurrence with COUNT

    /// <summary>
    /// Tests that GetEndDate returns the last occurrence end for a daily recurrence with COUNT.
    /// </summary>
    [Fact]
    public async Task GivenDailyRecurrenceWithCount5_WhenCreateEventAsync_ThenEndDateIsLastOccurrenceEnd()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(startUtc.AddHours(1))
        };
        var rule = new RecurrencePattern(FrequencyType.Daily, 1) { Count = 5 };
        entry.RecurrenceRules.Add(rule);

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        // 5 occurrences: June 15, 16, 17, 18, 19
        // Last occurrence ends at June 19, 11:00 UTC
        var expectedEndDate = new DateTime(2025, 6, 19, 11, 0, 0, DateTimeKind.Utc);
        Assert.NotNull(capturedMetadata);
        Assert.NotNull(capturedMetadata.EndDate);
        Assert.Equal(DateTimeKind.Utc, capturedMetadata.EndDate.Value.Kind);
        Assert.Equal(expectedEndDate, capturedMetadata.EndDate.Value);
    }

    /// <summary>
    /// Tests that GetEndDate returns the last occurrence end for a weekly recurrence with COUNT.
    /// </summary>
    [Fact]
    public async Task GivenWeeklyRecurrenceWithCount3_WhenCreateEventAsync_ThenEndDateIsLastOccurrenceEnd()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 14, 0, 0, DateTimeKind.Utc); // Sunday
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(startUtc.AddHours(2))
        };
        var rule = new RecurrencePattern(FrequencyType.Weekly, 1) { Count = 3 };
        entry.RecurrenceRules.Add(rule);

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        // 3 weekly occurrences: June 15, June 22, June 29
        // Last occurrence ends at June 29, 16:00 UTC
        var expectedEndDate = new DateTime(2025, 6, 29, 16, 0, 0, DateTimeKind.Utc);
        Assert.NotNull(capturedMetadata);
        Assert.NotNull(capturedMetadata.EndDate);
        Assert.Equal(expectedEndDate, capturedMetadata.EndDate.Value);
    }

    #endregion

    #region GetEndDate Tests - Finite Recurrence with UNTIL

    /// <summary>
    /// Tests that GetEndDate returns the last occurrence end for a daily recurrence with UNTIL.
    /// </summary>
    [Fact]
    public async Task GivenDailyRecurrenceWithUntil_WhenCreateEventAsync_ThenEndDateIsLastOccurrenceEnd()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var untilUtc = new DateTime(2025, 6, 18, 23, 59, 59, DateTimeKind.Utc);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(startUtc.AddMinutes(30))
        };
        var rule = new RecurrencePattern(FrequencyType.Daily, 1) { Until = new CalDateTime(untilUtc) };
        entry.RecurrenceRules.Add(rule);

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        // Occurrences: June 15, 16, 17, 18 (until June 18 23:59:59)
        // Last occurrence ends at June 18, 10:30 UTC
        var expectedEndDate = new DateTime(2025, 6, 18, 10, 30, 0, DateTimeKind.Utc);
        Assert.NotNull(capturedMetadata);
        Assert.NotNull(capturedMetadata.EndDate);
        Assert.Equal(expectedEndDate, capturedMetadata.EndDate.Value);
    }

    #endregion

    #region GetEndDate Tests - Edge Cases

    /// <summary>
    /// Tests that GetEndDate handles multiple recurrence rules where all are infinite.
    /// </summary>
    [Fact]
    public async Task GivenMultipleInfiniteRecurrenceRules_WhenCreateEventAsync_ThenEndDateIsNull()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(startUtc.AddHours(1))
        };
        entry.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1)); // Infinite
        entry.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Weekly, 1)); // Infinite

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.Null(capturedMetadata.EndDate);
    }

    /// <summary>
    /// Tests that GetEndDate returns null for a Journal without recurrence and without start.
    /// </summary>
    [Fact]
    public async Task GivenJournalWithNoStartOrRecurrence_WhenCreateEventAsync_ThenEndDateIsNull()
    {
        // Arrange
        var entry = new Journal { Uid = "journal-uid" };

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.Null(capturedMetadata.EndDate);
    }

    /// <summary>
    /// Tests that GetEndDate handles a recurrence with COUNT of 1 (single occurrence).
    /// </summary>
    [Fact]
    public async Task GivenRecurrenceWithCount1_WhenCreateEventAsync_ThenEndDateIsFirstOccurrenceEnd()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(startUtc.AddHours(1))
        };
        var rule = new RecurrencePattern(FrequencyType.Daily, 1) { Count = 1 };
        entry.RecurrenceRules.Add(rule);

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        var expectedEndDate = new DateTime(2025, 6, 15, 11, 0, 0, DateTimeKind.Utc);
        Assert.NotNull(capturedMetadata);
        Assert.NotNull(capturedMetadata.EndDate);
        Assert.Equal(expectedEndDate, capturedMetadata.EndDate.Value);
    }

    /// <summary>
    /// Tests that GetEndDate handles yearly recurrence with COUNT.
    /// </summary>
    [Fact]
    public async Task GivenYearlyRecurrenceWithCount3_WhenCreateEventAsync_ThenEndDateIsLastOccurrenceEnd()
    {
        // Arrange
        var startUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(startUtc),
            End = new CalDateTime(startUtc.AddDays(1))
        };
        var rule = new RecurrencePattern(FrequencyType.Yearly, 1) { Count = 3 };
        entry.RecurrenceRules.Add(rule);

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        // 3 yearly occurrences: 2025, 2026, 2027
        // Last occurrence ends at Jan 2, 2027
        var expectedEndDate = new DateTime(2027, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        Assert.NotNull(capturedMetadata);
        Assert.NotNull(capturedMetadata.EndDate);
        Assert.Equal(expectedEndDate, capturedMetadata.EndDate.Value);
    }

    /// <summary>
    /// Tests that GetEndDate handles an all-day event with recurrence.
    /// </summary>
    [Fact]
    public async Task GivenAllDayEventWithRecurrence_WhenCreateEventAsync_ThenEndDateIsLastOccurrenceEnd()
    {
        // Arrange
        // Use date-only CalDateTime constructors for all-day events
        var entry = new CalendarEvent
        {
            Uid = "test-uid",
            Start = new CalDateTime(2025, 6, 15),  // Date-only (no time component)
            End = new CalDateTime(2025, 6, 16)     // Date-only (no time component)
        };
        var rule = new RecurrencePattern(FrequencyType.Daily, 1) { Count = 3 };
        entry.RecurrenceRules.Add(rule);

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        // 3 all-day occurrences: June 15, 16, 17
        // Last occurrence ends at midnight June 18
        Assert.NotNull(capturedMetadata);
        Assert.NotNull(capturedMetadata.EndDate);
        Assert.Equal(new DateTime(2025, 6, 18, 0, 0, 0, DateTimeKind.Utc), capturedMetadata.EndDate.Value);
    }

    /// <summary>
    /// Tests that GetEndDate handles Todo with due date and recurrence with COUNT.
    /// </summary>
    [Fact]
    public async Task GivenTodoWithRecurrence_WhenCreateEventAsync_ThenEndDateIsLastOccurrenceEnd()
    {
        // Arrange
        var startUtc = new DateTime(2025, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var dueUtc = new DateTime(2025, 6, 15, 17, 0, 0, DateTimeKind.Utc);
        var entry = new Todo
        {
            Uid = "todo-uid",
            Start = new CalDateTime(startUtc),
            Due = new CalDateTime(dueUtc)
        };
        var rule = new RecurrencePattern(FrequencyType.Daily, 1) { Count = 2 };
        entry.RecurrenceRules.Add(rule);

        EventMetadata? capturedMetadata = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(
                It.IsAny<EventMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventMetadata, CancellationToken>(
                (metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CreateEventAsync(entry, "tenant1");

        // Assert
        // 2 occurrences: June 15, June 16
        // Last occurrence ends at June 16, 17:00 UTC (due time)
        var expectedEndDate = new DateTime(2025, 6, 16, 17, 0, 0, DateTimeKind.Utc);
        Assert.NotNull(capturedMetadata);
        Assert.NotNull(capturedMetadata.EndDate);
        Assert.Equal(expectedEndDate, capturedMetadata.EndDate.Value);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Tests that RecurrenceEngine constructor throws ArgumentNullException when repository is null.
    /// </summary>
    [Fact]
    public void GivenNullRepository_WhenConstructed_ThenThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new RecurrenceEngine(null!));
        Assert.Equal("repository", exception.ParamName);
    }

    #endregion
}
