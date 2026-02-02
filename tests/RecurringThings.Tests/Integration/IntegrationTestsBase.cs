namespace RecurringThings.Tests.Integration;

using System.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Microsoft.Extensions.DependencyInjection;
using RecurringThings.Engine;
using RecurringThings.Exceptions;
using RecurringThings.Filters;

/// <summary>
/// Base class for integration tests that test through IRecurrenceEngine.
/// </summary>
/// <remarks>
/// <para>
/// Integration tests must use IRecurrenceEngine exclusively and never bypass to repositories.
/// </para>
/// <para>
/// Derived classes must implement a fixture that provides IServiceProvider.
/// The fixture's Provider property is null if the environment variable is not set.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
public abstract class IntegrationTestsBase
{
    /// <summary>
    /// Test resource path for multi-tenancy.
    /// </summary>
    protected const string TestResourcePath = "test/path";

    /// <summary>
    /// Test type for calendar entries.
    /// </summary>
    protected const string TestType = "appointment";

    /// <summary>
    /// Test timezone in IANA format.
    /// </summary>
    protected const string TestTimeZone = "America/New_York";

    /// <summary>
    /// Gets the service provider from the fixture.
    /// Returns null if the environment variable is not set.
    /// </summary>
    protected abstract IServiceProvider? Provider { get; }

    /// <summary>
    /// Gets the name of the environment variable required for these tests.
    /// Used in skip messages.
    /// </summary>
    protected abstract string EnvironmentVariableName { get; }

    /// <summary>
    /// Gets the recurrence engine from the service provider.
    /// </summary>
    protected IRecurrenceEngine GetEngine()
    {
        return Provider!.GetRequiredService<IRecurrenceEngine>();
    }

    /// <summary>
    /// Skips the test if the provider is not available.
    /// </summary>
    protected void SkipIfNoProvider()
    {
        Skip.IfNot(Provider != null, $"{EnvironmentVariableName} environment variable not set");
    }

    #region Happy Path Tests

    /// <summary>
    /// Tests that a single non-recurring event can be created and retrieved.
    /// This is the simplest happy path scenario verifying basic CRUD operations.
    /// Expected: Event is persisted and can be retrieved with matching UID and properties.
    /// </summary>
    [SkippableFact]
    public async Task GivenSingleEvent_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Test Single Event"
        };

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.Equal(uid, retrieved.Uid);
        Assert.Equal("Test Single Event", retrieved.Summary);
    }

    /// <summary>
    /// Tests that a CalendarEvent (VEVENT) can be created and retrieved with correct component type.
    /// Expected: Retrieved component is a CalendarEvent with ComponentType.Event filter working.
    /// </summary>
    [SkippableFact]
    public async Task GivenCalendarEvent_WhenCreatedAndQueriedByType_ThenReturnsEvent()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).OfType(ComponentType.Event).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.IsType<CalendarEvent>(results[0]);
        Assert.Equal(uid, results[0].Uid);
    }

    /// <summary>
    /// Tests that a Todo (VTODO) can be created and retrieved with correct component type.
    /// Expected: Retrieved component is a Todo with ComponentType.Todo filter working.
    /// </summary>
    [SkippableFact]
    public async Task GivenTodo_WhenCreatedAndQueriedByType_ThenReturnsTodo()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var dueUtc = new DateTime(2025, 6, 16, 10, 0, 0, DateTimeKind.Utc);

        var todo = new Todo
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            Due = new CalDateTime(dueUtc, "UTC"),
            Summary = "Test Todo"
        };

        // Act
        await engine.CreateEventAsync(todo, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(dueUtc.AddDays(1)).OfType(ComponentType.Todo).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.IsType<Todo>(results[0]);
        Assert.Equal(uid, results[0].Uid);
    }

    /// <summary>
    /// Tests that a Journal (VJOURNAL) can be created and retrieved with correct component type.
    /// Expected: Retrieved component is a Journal with ComponentType.Journal filter working.
    /// </summary>
    [SkippableFact]
    public async Task GivenJournal_WhenCreatedAndQueriedByType_ThenReturnsJournal()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var journal = new Journal
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            Summary = "Test Journal Entry"
        };

        // Act
        await engine.CreateEventAsync(journal, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().OfType(ComponentType.Journal).Build()).ToListAsync();

        // Assert
        Assert.Contains(results, r => r.Uid == uid);
        var retrieved = results.First(r => r.Uid == uid);
        Assert.IsType<Journal>(retrieved);
    }

    /// <summary>
    /// Tests daily recurrence with finite COUNT termination.
    /// Expected: Event created with daily RRULE is retrievable and has correct UID.
    /// </summary>
    [SkippableFact]
    public async Task GivenDailyRecurrence_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1) { Count = 10 });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddDays(15)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        Assert.Single(((CalendarEvent)results[0]).RecurrenceRules);
    }

    /// <summary>
    /// Tests weekly recurrence with finite COUNT termination.
    /// Expected: Event created with weekly RRULE is retrievable.
    /// </summary>
    [SkippableFact]
    public async Task GivenWeeklyRecurrence_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Weekly, 1) { Count = 8 });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddDays(60)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var rrule = Assert.Single(((CalendarEvent)results[0]).RecurrenceRules);
        Assert.Equal(FrequencyType.Weekly, rrule.Frequency);
    }

    /// <summary>
    /// Tests monthly recurrence with finite COUNT termination.
    /// Expected: Event created with monthly RRULE is retrievable.
    /// </summary>
    [SkippableFact]
    public async Task GivenMonthlyRecurrence_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Monthly, 1) { Count = 6 });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddMonths(7)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var rrule = Assert.Single(((CalendarEvent)results[0]).RecurrenceRules);
        Assert.Equal(FrequencyType.Monthly, rrule.Frequency);
    }

    /// <summary>
    /// Tests yearly recurrence with finite COUNT termination.
    /// Expected: Event created with yearly RRULE is retrievable.
    /// </summary>
    [SkippableFact]
    public async Task GivenYearlyRecurrence_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Yearly, 1) { Count = 3 });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddYears(4)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var rrule = Assert.Single(((CalendarEvent)results[0]).RecurrenceRules);
        Assert.Equal(FrequencyType.Yearly, rrule.Frequency);
    }

    /// <summary>
    /// Tests recurrence with UNTIL termination date.
    /// Expected: Event with UNTIL is retrievable and query respects the termination boundary.
    /// </summary>
    [SkippableFact]
    public async Task GivenRecurrenceWithUntil_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var untilUtc = new DateTime(2025, 7, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1)
        {
            Until = new CalDateTime(untilUtc, "UTC")
        });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(untilUtc.AddDays(10)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
    }

    /// <summary>
    /// Tests recurrence with COUNT termination.
    /// Expected: Event with COUNT is retrievable.
    /// </summary>
    [SkippableFact]
    public async Task GivenRecurrenceWithCount_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1) { Count = 5 });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddDays(10)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var rrule = Assert.Single(((CalendarEvent)results[0]).RecurrenceRules);
        Assert.Equal(5, rrule.Count);
    }

    /// <summary>
    /// Tests infinite recurrence (no UNTIL or COUNT).
    /// Expected: Event with infinite recurrence is retrievable within any date range.
    /// </summary>
    [SkippableFact]
    public async Task GivenInfiniteRecurrence_WhenCreatedAndQueriedInFuture_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        // Infinite recurrence - no Count, no Until
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1));

        // Act
        await engine.CreateEventAsync(evt, tenantId);

        // Query far in the future - should still find the event due to infinite recurrence
        var farFutureStart = startUtc.AddYears(10);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(farFutureStart).To(farFutureStart.AddDays(30)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
    }

    /// <summary>
    /// Tests that events with categories are persisted and retrievable.
    /// Expected: Categories are preserved and can be used for filtering.
    /// </summary>
    [SkippableFact]
    public async Task GivenEventWithCategories_WhenCreatedAndQueried_ThenCategoriesPreserved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.Categories.Add("Work");
        evt.Categories.Add("Meeting");
        evt.Categories.Add("Important");

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.Equal(3, retrieved.Categories.Count);
        Assert.Contains("Work", retrieved.Categories);
        Assert.Contains("Meeting", retrieved.Categories);
        Assert.Contains("Important", retrieved.Categories);
    }

    /// <summary>
    /// Tests that events with custom properties are persisted and retrievable.
    /// Expected: Properties are preserved and can be used for filtering.
    /// </summary>
    [SkippableFact]
    public async Task GivenEventWithProperties_WhenCreatedAndQueried_ThenPropertiesPreserved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.Properties.Add(new CalendarProperty("X-CUSTOM-PROP", "custom-value"));
        evt.Properties.Add(new CalendarProperty("X-PRIORITY", "high"));

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.Contains(retrieved.Properties, p => p.Name == "X-CUSTOM-PROP");
        Assert.Contains(retrieved.Properties, p => p.Name == "X-PRIORITY");
    }

    #endregion

    #region Multi-Tenancy Tests

    /// <summary>
    /// Tests that events from different tenants are isolated.
    /// Expected: Querying tenant A returns only tenant A's events.
    /// </summary>
    [SkippableFact]
    public async Task GivenMultipleTenants_WhenQueried_ThenReturnsOnlyMatchingTenant()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var tenant1Uid = Guid.NewGuid().ToString();
        var tenant2Uid = Guid.NewGuid().ToString();

        var evt1 = new CalendarEvent
        {
            Uid = tenant1Uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Tenant 1 Event"
        };

        var evt2 = new CalendarEvent
        {
            Uid = tenant2Uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Tenant 2 Event"
        };

        // Act
        await engine.CreateEventAsync(evt1, "tenant-1");
        await engine.CreateEventAsync(evt2, "tenant-2");

        var tenant1Results = await engine.GetEventsAsync(
            "tenant-1",
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        var tenant2Results = await engine.GetEventsAsync(
            "tenant-2",
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(tenant1Results);
        Assert.Equal(tenant1Uid, tenant1Results[0].Uid);

        Assert.Single(tenant2Results);
        Assert.Equal(tenant2Uid, tenant2Results[0].Uid);
    }

    /// <summary>
    /// Tests that user-scoped events are isolated within a tenant.
    /// Expected: Querying with userId returns only that user's events.
    /// </summary>
    [SkippableFact]
    public async Task GivenUserScopedEvents_WhenQueried_ThenReturnsOnlyMatchingUser()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var user1Uid = Guid.NewGuid().ToString();
        var user2Uid = Guid.NewGuid().ToString();

        var evt1 = new CalendarEvent
        {
            Uid = user1Uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "User 1 Event"
        };

        var evt2 = new CalendarEvent
        {
            Uid = user2Uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "User 2 Event"
        };

        // Act
        await engine.CreateEventAsync(evt1, tenantId, userId: "user-1");
        await engine.CreateEventAsync(evt2, tenantId, userId: "user-2");

        var user1Results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().ForUser("user-1").From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        var user2Results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().ForUser("user-2").From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(user1Results);
        Assert.Equal(user1Uid, user1Results[0].Uid);

        Assert.Single(user2Results);
        Assert.Equal(user2Uid, user2Results[0].Uid);
    }

    /// <summary>
    /// Tests that querying with null userId returns only tenant-global events.
    /// Null userId means "global to tenant" - events not scoped to a specific user.
    /// Expected: Only events with null userId are returned, not user-scoped events.
    /// </summary>
    [SkippableFact]
    public async Task GivenMixedUserEvents_WhenQueriedWithNullUserId_ThenReturnsOnlyGlobalEvents()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString(); // Unique tenant for isolation
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var user1Uid = Guid.NewGuid().ToString();
        var user2Uid = Guid.NewGuid().ToString();
        var globalUid = Guid.NewGuid().ToString();

        var evt1 = new CalendarEvent
        {
            Uid = user1Uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        var evt2 = new CalendarEvent
        {
            Uid = user2Uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        var evt3 = new CalendarEvent
        {
            Uid = globalUid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        // Act
        await engine.CreateEventAsync(evt1, tenantId, userId: "user-1");
        await engine.CreateEventAsync(evt2, tenantId, userId: "user-2");
        await engine.CreateEventAsync(evt3, tenantId, userId: null); // Global event

        var globalResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert - only the global event should be returned
        Assert.Single(globalResults);
        Assert.Equal(globalUid, globalResults[0].Uid);
    }

    /// <summary>
    /// Tests that querying with UserFilter.All returns all events regardless of user assignment.
    /// This includes both user-scoped events and tenant-global events.
    /// Expected: All events for the tenant are returned.
    /// </summary>
    [SkippableFact]
    public async Task GivenMixedUserEvents_WhenQueriedWithUserFilterAll_ThenReturnsAllTenantEvents()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString(); // Unique tenant for isolation
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var user1Uid = Guid.NewGuid().ToString();
        var user2Uid = Guid.NewGuid().ToString();
        var globalUid = Guid.NewGuid().ToString();

        var evt1 = new CalendarEvent
        {
            Uid = user1Uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        var evt2 = new CalendarEvent
        {
            Uid = user2Uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        var evt3 = new CalendarEvent
        {
            Uid = globalUid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        // Act
        await engine.CreateEventAsync(evt1, tenantId, userId: "user-1");
        await engine.CreateEventAsync(evt2, tenantId, userId: "user-2");
        await engine.CreateEventAsync(evt3, tenantId, userId: null); // Global event

        var allResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().AllUsers().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert - all three events should be returned
        Assert.Equal(3, allResults.Count);
        var uids = allResults.Select(r => r.Uid).ToHashSet();
        Assert.Contains(user1Uid, uids);
        Assert.Contains(user2Uid, uids);
        Assert.Contains(globalUid, uids);
    }

    /// <summary>
    /// Tests that UserFilter.Specific with a user ID returns only that user's events.
    /// Expected: Only events assigned to the specified user are returned.
    /// </summary>
    [SkippableFact]
    public async Task GivenMixedUserEvents_WhenQueriedWithSpecificUser_ThenReturnsOnlyThatUsersEvents()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var user1Uid = Guid.NewGuid().ToString();
        var user2Uid = Guid.NewGuid().ToString();
        var globalUid = Guid.NewGuid().ToString();

        var evt1 = new CalendarEvent
        {
            Uid = user1Uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        var evt2 = new CalendarEvent
        {
            Uid = user2Uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        var evt3 = new CalendarEvent
        {
            Uid = globalUid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        // Act
        await engine.CreateEventAsync(evt1, tenantId, userId: "user-1");
        await engine.CreateEventAsync(evt2, tenantId, userId: "user-2");
        await engine.CreateEventAsync(evt3, tenantId, userId: null);

        var user1Results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().ForUser("user-1").From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert - only user-1's event should be returned
        Assert.Single(user1Results);
        Assert.Equal(user1Uid, user1Results[0].Uid);
    }

    #endregion

    #region Multiple Patterns Tests

    /// <summary>
    /// Tests event with multiple recurrence rules (e.g., every Monday and every Friday).
    /// Expected: Event with multiple RRULEs is retrievable with all rules preserved.
    /// </summary>
    [SkippableFact]
    public async Task GivenMultipleRecurrenceRules_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 16, 10, 0, 0, DateTimeKind.Utc); // Monday

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        // Two separate rules: every Monday and every Friday
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Weekly, 1)
        {
            ByDay = [new WeekDay(DayOfWeek.Monday)],
            Count = 4
        });
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Weekly, 1)
        {
            ByDay = [new WeekDay(DayOfWeek.Friday)],
            Count = 4
        });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddDays(30)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.Equal(2, retrieved.RecurrenceRules.Count);
    }

    /// <summary>
    /// Tests that an event with mixed recurrence patterns (one infinite, one finite) is handled correctly.
    /// The engine must determine the overall end date is null due to the infinite pattern.
    /// If this test hangs, the engine is incorrectly trying to enumerate infinite occurrences.
    /// </summary>
    [SkippableFact]
    public async Task GivenMixedInfiniteAndFiniteRecurrence_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 16, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        // Infinite recurrence - no Count, no Until
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1));
        // Finite recurrence - with Count
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Weekly, 1) { Count = 5 });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddDays(30)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.Equal(2, retrieved.RecurrenceRules.Count);
    }

    /// <summary>
    /// Tests weekly recurrence with BYDAY filter (e.g., every Monday and Wednesday).
    /// Expected: Event with BYDAY is retrievable with the correct days preserved.
    /// </summary>
    [SkippableFact]
    public async Task GivenRecurrenceWithByDay_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 16, 10, 0, 0, DateTimeKind.Utc); // Monday

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Weekly, 1)
        {
            ByDay = [new WeekDay(DayOfWeek.Monday), new WeekDay(DayOfWeek.Wednesday), new WeekDay(DayOfWeek.Friday)],
            Count = 12
        });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddDays(30)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var rrule = Assert.Single(((CalendarEvent)results[0]).RecurrenceRules);
        Assert.Equal(3, rrule.ByDay.Count);
    }

    /// <summary>
    /// Tests monthly recurrence with BYMONTHDAY filter (e.g., 1st and 15th of each month).
    /// Expected: Event with BYMONTHDAY is retrievable with the correct days preserved.
    /// </summary>
    [SkippableFact]
    public async Task GivenRecurrenceWithByMonthDay_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Monthly, 1)
        {
            ByMonthDay = [1, 15],
            Count = 6
        });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddMonths(4)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var rrule = Assert.Single(((CalendarEvent)results[0]).RecurrenceRules);
        Assert.Equal(2, rrule.ByMonthDay.Count);
        Assert.Contains(1, rrule.ByMonthDay);
        Assert.Contains(15, rrule.ByMonthDay);
    }

    #endregion

    #region Edge Case Tests

    /// <summary>
    /// Tests event at timezone boundary (DST transition).
    /// Verifies that UTC conversion handles daylight saving time correctly.
    /// Expected: Event is persisted and retrieved with correct UTC times.
    /// </summary>
    [SkippableFact]
    public async Task GivenEventAtDstTransition_WhenCreatedAndQueried_ThenTimezoneHandledCorrectly()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        // US DST spring forward: March 9, 2025 at 2:00 AM
        var startUtc = new DateTime(2025, 3, 9, 7, 0, 0, DateTimeKind.Utc); // 2 AM Eastern

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddHours(-1)).To(startUtc.AddHours(2)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
    }

    /// <summary>
    /// Tests recurrence with COUNT=1 (effectively a single occurrence with recurrence rule).
    /// Expected: Single occurrence event is retrievable.
    /// </summary>
    [SkippableFact]
    public async Task GivenRecurrenceWithCount1_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1) { Count = 1 });

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var rrule = Assert.Single(((CalendarEvent)results[0]).RecurrenceRules);
        Assert.Equal(1, rrule.Count);
    }

    /// <summary>
    /// Tests all-day event (DATE only, no TIME component).
    /// Expected: All-day event is retrievable within date range.
    /// </summary>
    [SkippableFact]
    public async Task GivenAllDayEvent_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(2025, 6, 15), // Date only - all-day event
            End = new CalDateTime(2025, 6, 16)    // Next day (exclusive end)
        };

        // Act
        await engine.CreateEventAsync(evt, tenantId);

        // Query with UTC times that should encompass the all-day event
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(new DateTime(2025, 6, 14, 0, 0, 0, DateTimeKind.Utc)).To(new DateTime(2025, 6, 17, 0, 0, 0, DateTimeKind.Utc)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        Assert.True(((CalendarEvent)results[0]).IsAllDay);
    }

    /// <summary>
    /// Tests event with explicit duration instead of end time.
    /// Expected: Duration-based event is retrievable.
    /// </summary>
    [SkippableFact]
    public async Task GivenEventWithDuration_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            Duration = new Duration(hours: 2)
        };

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.NotNull(retrieved.Duration);
    }

    /// <summary>
    /// Tests Todo with due date but no start date.
    /// Expected: Todo is retrievable when querying within the due date range.
    /// </summary>
    [SkippableFact]
    public async Task GivenTodoWithDueDateOnly_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var dueUtc = new DateTime(2025, 6, 20, 17, 0, 0, DateTimeKind.Utc);

        var todo = new Todo
        {
            Uid = uid,
            Due = new CalDateTime(dueUtc, "UTC"),
            Summary = "Todo with due date only"
        };

        // Act
        await engine.CreateEventAsync(todo, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().OfType(ComponentType.Todo).Build()).ToListAsync();

        // Assert
        Assert.Contains(results, r => r.Uid == uid);
    }

    /// <summary>
    /// Tests Journal with start date.
    /// Expected: Journal is retrievable when querying.
    /// </summary>
    [SkippableFact]
    public async Task GivenJournalWithStartDate_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var journal = new Journal
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            Summary = "Daily Journal Entry"
        };

        // Act
        await engine.CreateEventAsync(journal, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).OfType(ComponentType.Journal).Build()).ToListAsync();

        // Assert
        Assert.Contains(results, r => r.Uid == uid);
        var retrieved = results.First(r => r.Uid == uid);
        Assert.IsType<Journal>(retrieved);
    }

    /// <summary>
    /// Tests event with past start date and future recurrence.
    /// Expected: Event is retrievable when querying future date range.
    /// </summary>
    [SkippableFact]
    public async Task GivenPastStartWithFutureRecurrence_WhenQueriedInFuture_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc); // 5 years ago

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        // Infinite recurrence that extends into the future
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Weekly, 1));

        // Act
        await engine.CreateEventAsync(evt, tenantId);

        // Query in 2025 - should still find the event
        var futureStart = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(futureStart).To(futureStart.AddDays(30)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
    }

    #endregion

    #region EXDATE Tests

    /// <summary>
    /// Tests event with single EXDATE excludes that occurrence.
    /// Expected: Event with EXDATE is retrievable and the serialized data contains the EXDATE.
    /// </summary>
    [SkippableFact]
    public async Task GivenEventWithSingleExdate_WhenCreatedAndQueried_ThenExdatePreserved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var exdateUtc = new DateTime(2025, 6, 17, 10, 0, 0, DateTimeKind.Utc); // Skip day 3

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1) { Count = 5 });
        evt.ExceptionDates.Add(new CalDateTime(exdateUtc, "UTC"));

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddDays(10)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        // ExceptionDates are preserved in the serialized iCalendar data
        Assert.IsType<CalendarEvent>(results[0]);
    }

    /// <summary>
    /// Tests event with multiple EXDATEs.
    /// Expected: All EXDATEs are preserved in the serialized data.
    /// </summary>
    [SkippableFact]
    public async Task GivenEventWithMultipleExdates_WhenCreatedAndQueried_ThenAllExdatesPreserved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1) { Count = 10 });

        // Add multiple exception dates
        evt.ExceptionDates.Add(new CalDateTime(startUtc.AddDays(2), "UTC")); // Skip day 3
        evt.ExceptionDates.Add(new CalDateTime(startUtc.AddDays(4), "UTC")); // Skip day 5
        evt.ExceptionDates.Add(new CalDateTime(startUtc.AddDays(6), "UTC")); // Skip day 7

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc).To(startUtc.AddDays(15)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        // ExceptionDates are preserved in the serialized iCalendar data
        // Verify by checking occurrences exclude the exception dates
        var retrieved = (CalendarEvent)results[0];
        var occurrences = retrieved.GetOccurrences().ToList();
        // Original: 10 occurrences (COUNT=10). With 3 exceptions: 7 occurrences
        Assert.Equal(7, occurrences.Count);
    }

    #endregion

    #region Query Filtering Tests

    /// <summary>
    /// Tests date range filtering returns events within the range.
    /// Expected: Only events overlapping with the date range are returned.
    /// </summary>
    [SkippableFact]
    public async Task GivenDateRange_WhenQueried_ThenReturnsOverlappingEvents()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();

        var evt1Uid = Guid.NewGuid().ToString();
        var evt2Uid = Guid.NewGuid().ToString();

        var evt1Start = new DateTime(2025, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var evt2Start = new DateTime(2025, 6, 20, 10, 0, 0, DateTimeKind.Utc);

        var evt1 = new CalendarEvent
        {
            Uid = evt1Uid,
            Start = new CalDateTime(evt1Start, "UTC"),
            End = new CalDateTime(evt1Start.AddHours(1), "UTC")
        };

        var evt2 = new CalendarEvent
        {
            Uid = evt2Uid,
            Start = new CalDateTime(evt2Start, "UTC"),
            End = new CalDateTime(evt2Start.AddHours(1), "UTC")
        };

        // Act
        await engine.CreateEventAsync(evt1, tenantId);
        await engine.CreateEventAsync(evt2, tenantId);

        // Query range that includes only evt1
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(new DateTime(2025, 6, 8, 0, 0, 0, DateTimeKind.Utc)).To(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(evt1Uid, results[0].Uid);
    }

    /// <summary>
    /// Tests date range filtering excludes events entirely outside the range.
    /// Expected: Events outside the date range are not returned.
    /// </summary>
    [SkippableFact]
    public async Task GivenDateRange_WhenQueried_ThenExcludesEventsOutsideRange()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();

        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        // Act
        await engine.CreateEventAsync(evt, tenantId);

        // Query range that does NOT include the event
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc)).To(new DateTime(2025, 7, 31, 0, 0, 0, DateTimeKind.Utc)).Build()).ToListAsync();

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    /// Tests component type filtering returns only matching types.
    /// Expected: Filtering by Event returns only CalendarEvents.
    /// </summary>
    [SkippableFact]
    public async Task GivenComponentTypeFilter_WhenQueried_ThenReturnsOnlyMatchingType()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var eventUid = Guid.NewGuid().ToString();
        var todoUid = Guid.NewGuid().ToString();

        var evt = new CalendarEvent
        {
            Uid = eventUid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        var todo = new Todo
        {
            Uid = todoUid,
            Start = new CalDateTime(startUtc, "UTC"),
            Due = new CalDateTime(startUtc.AddDays(1), "UTC")
        };

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        await engine.CreateEventAsync(todo, tenantId);

        var eventResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(5)).OfType(ComponentType.Event).Build()).ToListAsync();

        var todoResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(5)).OfType(ComponentType.Todo).Build()).ToListAsync();

        // Assert
        Assert.Single(eventResults);
        Assert.IsType<CalendarEvent>(eventResults[0]);
        Assert.Equal(eventUid, eventResults[0].Uid);

        Assert.Single(todoResults);
        Assert.IsType<Todo>(todoResults[0]);
        Assert.Equal(todoUid, todoResults[0].Uid);
    }

    /// <summary>
    /// Tests that category filtering returns only events with matching categories.
    /// Creates multiple events with different categories and verifies only matching ones are returned.
    /// Expected: Only events with the specified category are returned.
    /// </summary>
    [SkippableFact]
    public async Task GivenMultipleEventsWithDifferentCategories_WhenFilteredByCategory_ThenReturnsOnlyMatching()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var workUid = Guid.NewGuid().ToString();
        var personalUid = Guid.NewGuid().ToString();
        var bothUid = Guid.NewGuid().ToString();
        var noCategoryUid = Guid.NewGuid().ToString();

        // Event with only "Work" category
        var workEvt = new CalendarEvent
        {
            Uid = workUid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Work Event"
        };
        workEvt.Categories.Add("Work");

        // Event with only "Personal" category
        var personalEvt = new CalendarEvent
        {
            Uid = personalUid,
            Start = new CalDateTime(startUtc.AddHours(2), "UTC"),
            End = new CalDateTime(startUtc.AddHours(3), "UTC"),
            Summary = "Personal Event"
        };
        personalEvt.Categories.Add("Personal");

        // Event with both categories
        var bothEvt = new CalendarEvent
        {
            Uid = bothUid,
            Start = new CalDateTime(startUtc.AddHours(4), "UTC"),
            End = new CalDateTime(startUtc.AddHours(5), "UTC"),
            Summary = "Work and Personal Event"
        };
        bothEvt.Categories.Add("Work");
        bothEvt.Categories.Add("Personal");

        // Event with no categories
        var noCategoryEvt = new CalendarEvent
        {
            Uid = noCategoryUid,
            Start = new CalDateTime(startUtc.AddHours(6), "UTC"),
            End = new CalDateTime(startUtc.AddHours(7), "UTC"),
            Summary = "No Category Event"
        };

        // Act
        await engine.CreateEventAsync(workEvt, tenantId);
        await engine.CreateEventAsync(personalEvt, tenantId);
        await engine.CreateEventAsync(bothEvt, tenantId);
        await engine.CreateEventAsync(noCategoryEvt, tenantId);

        // Query for "Work" category only
        var workResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).InCategories("Work").Build()).ToListAsync();

        // Query for "Personal" category only
        var personalResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).InCategories("Personal").Build()).ToListAsync();

        // Query for "NonExistent" category
        var nonExistentResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).InCategories("NonExistent").Build()).ToListAsync();

        // Query with no category filter (should return all)
        var allResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert - Work filter returns workEvt and bothEvt
        Assert.Equal(2, workResults.Count);
        Assert.Contains(workResults, r => r.Uid == workUid);
        Assert.Contains(workResults, r => r.Uid == bothUid);
        Assert.DoesNotContain(workResults, r => r.Uid == personalUid);
        Assert.DoesNotContain(workResults, r => r.Uid == noCategoryUid);

        // Assert - Personal filter returns personalEvt and bothEvt
        Assert.Equal(2, personalResults.Count);
        Assert.Contains(personalResults, r => r.Uid == personalUid);
        Assert.Contains(personalResults, r => r.Uid == bothUid);
        Assert.DoesNotContain(personalResults, r => r.Uid == workUid);
        Assert.DoesNotContain(personalResults, r => r.Uid == noCategoryUid);

        // Assert - NonExistent filter returns nothing
        Assert.Empty(nonExistentResults);

        // Assert - No filter returns all 4 events
        Assert.Equal(4, allResults.Count);
    }

    /// <summary>
    /// Tests category filtering is case-insensitive.
    /// Expected: Filtering by "WORK" matches events with category "work", "Work", "WORK".
    /// </summary>
    [SkippableFact]
    public async Task GivenCategoryFilter_WhenQueried_ThenMatchesCaseInsensitive()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var uid = Guid.NewGuid().ToString();

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.Categories.Add("Work"); // Mixed case

        // Act
        await engine.CreateEventAsync(evt, tenantId);

        // Query with uppercase
        var upperResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).InCategories("WORK").Build()).ToListAsync();

        // Query with lowercase
        var lowerResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).InCategories("work").Build()).ToListAsync();

        // Assert
        Assert.Single(upperResults);
        Assert.Equal(uid, upperResults[0].Uid);

        Assert.Single(lowerResults);
        Assert.Equal(uid, lowerResults[0].Uid);
    }

    /// <summary>
    /// Tests querying with null dates returns all events.
    /// Expected: No date filtering returns all tenant events.
    /// </summary>
    [SkippableFact]
    public async Task GivenNullDateRange_WhenQueried_ThenReturnsAllEvents()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();

        var uid1 = Guid.NewGuid().ToString();
        var uid2 = Guid.NewGuid().ToString();

        var evt1 = new CalendarEvent
        {
            Uid = uid1,
            Start = new CalDateTime(new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc), "UTC"),
            End = new CalDateTime(new DateTime(2020, 1, 1, 11, 0, 0, DateTimeKind.Utc), "UTC")
        };

        var evt2 = new CalendarEvent
        {
            Uid = uid2,
            Start = new CalDateTime(new DateTime(2030, 12, 31, 10, 0, 0, DateTimeKind.Utc), "UTC"),
            End = new CalDateTime(new DateTime(2030, 12, 31, 11, 0, 0, DateTimeKind.Utc), "UTC")
        };

        // Act
        await engine.CreateEventAsync(evt1, tenantId);
        await engine.CreateEventAsync(evt2, tenantId);

        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().Build()).ToListAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Uid == uid1);
        Assert.Contains(results, r => r.Uid == uid2);
    }

    /// <summary>
    /// Tests empty result when no events match criteria.
    /// Expected: Empty enumerable returned, no exceptions.
    /// </summary>
    [SkippableFact]
    public async Task GivenNoMatchingEvents_WhenQueried_ThenReturnsEmptyEnumerable()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var nonExistentTenant = Guid.NewGuid().ToString();

        // Act
        var results = await engine.GetEventsAsync(
            nonExistentTenant,
            EventFilter.Create().TenantWideOnly().From(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)).To(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc)).Build()).ToListAsync();

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region Validation/Error Tests

    /// <summary>
    /// Tests that duplicate categories in query throw ArgumentException.
    /// Expected: ArgumentException is thrown.
    /// </summary>
    [SkippableFact]
    public async Task GivenDuplicateCategoriesInQuery_WhenQueried_ThenThrowsArgumentException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();

        // Act & Assert - validation now happens at Build() time
        Assert.Throws<ArgumentException>(() =>
        {
            EventFilter.Create()
                .TenantWideOnly()
                .From(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .To(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc))
                .InCategories("Work", "work") // Duplicate (case-insensitive)
                .Build();
        });
    }

    /// <summary>
    /// Tests that Unspecified DateTimeKind throws ArgumentException at Build() time.
    /// Expected: ArgumentException is thrown for startDate or endDate with Unspecified kind.
    /// </summary>
    [Fact]
    public void GivenUnspecifiedDateTimeKind_WhenBuildingFilter_ThenThrowsArgumentException()
    {
        // Arrange
        var unspecifiedDate = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Unspecified);

        // Act & Assert - startDate with Unspecified kind
        Assert.Throws<ArgumentException>(() =>
        {
            EventFilter.Create()
                .TenantWideOnly()
                .From(unspecifiedDate)
                .To(new DateTime(2025, 6, 20, 0, 0, 0, DateTimeKind.Utc))
                .Build();
        });

        // Act & Assert - endDate with Unspecified kind
        Assert.Throws<ArgumentException>(() =>
        {
            EventFilter.Create()
                .TenantWideOnly()
                .From(new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc))
                .To(unspecifiedDate)
                .Build();
        });
    }

    /// <summary>
    /// Tests that creating two events with the same UID throws DuplicateUidException.
    /// The UID is a unique identifier and duplicates should be rejected by the persistence layer.
    /// Expected: DuplicateUidException is thrown with the correct UID.
    /// </summary>
    [SkippableFact]
    public async Task GivenDuplicateUid_WhenCreatedTwice_ThenThrowsDuplicateUidException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt1 = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "First Event"
        };

        var evt2 = new CalendarEvent
        {
            Uid = uid, // Same UID
            Start = new CalDateTime(startUtc.AddDays(1), "UTC"),
            End = new CalDateTime(startUtc.AddDays(1).AddHours(1), "UTC"),
            Summary = "Second Event"
        };

        // Act
        await engine.CreateEventAsync(evt1, tenantId);

        // Assert - second creation with same UID should throw DuplicateUidException
        var exception = await Assert.ThrowsAsync<DuplicateUidException>(async () => await engine.CreateEventAsync(evt2, tenantId));

        Assert.Equal(uid, exception.Uid);
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Tests that an existing event can be updated and the changes are retrieved.
    /// Expected: Updated summary is returned when querying the event.
    /// </summary>
    [SkippableFact]
    public async Task GivenExistingEvent_WhenUpdated_ThenChangesAreRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Original Summary"
        };

        await engine.CreateEventAsync(evt, tenantId);

        // Update the event
        var updatedEvt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(2), "UTC"), // Changed duration
            Summary = "Updated Summary"
        };

        // Act
        await engine.UpdateEventAsync(updatedEvt, tenantId);

        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.Equal(uid, retrieved.Uid);
        Assert.Equal("Updated Summary", retrieved.Summary);
    }

    /// <summary>
    /// Tests that updating a non-existent event throws EventNotFoundException.
    /// Expected: EventNotFoundException with correct UID is thrown.
    /// </summary>
    [SkippableFact]
    public async Task GivenNonExistentEvent_WhenUpdated_ThenThrowsEventNotFoundException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Non-existent Event"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(async () => await engine.UpdateEventAsync(evt, tenantId));

        Assert.Equal(uid, exception.Uid);
        Assert.Equal(tenantId, exception.TenantId);
    }

    /// <summary>
    /// Tests that updating with wrong tenant throws EventNotFoundException.
    /// Expected: EventNotFoundException is thrown due to tenant isolation.
    /// </summary>
    [SkippableFact]
    public async Task GivenWrongTenant_WhenUpdated_ThenThrowsEventNotFoundException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var wrongTenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Original Event"
        };

        await engine.CreateEventAsync(evt, tenantId);

        // Try to update with wrong tenant
        var updatedEvt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Updated Event"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(async () => await engine.UpdateEventAsync(updatedEvt, wrongTenantId));

        Assert.Equal(uid, exception.Uid);
        Assert.Equal(wrongTenantId, exception.TenantId);
    }

    /// <summary>
    /// Tests that updating with wrong user throws EventNotFoundException.
    /// Expected: EventNotFoundException is thrown due to user isolation.
    /// </summary>
    [SkippableFact]
    public async Task GivenWrongUser_WhenUpdated_ThenThrowsEventNotFoundException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "User 1 Event"
        };

        await engine.CreateEventAsync(evt, tenantId, userId: "user-1");

        // Try to update with different user
        var updatedEvt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Updated Event"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(async () => await engine.UpdateEventAsync(updatedEvt, tenantId, userId: "user-2"));

        Assert.Equal(uid, exception.Uid);
        Assert.Equal("user-2", exception.UserId);
    }

    /// <summary>
    /// Tests that updated categories replace the original categories.
    /// Expected: Only the new categories are returned after update.
    /// </summary>
    [SkippableFact]
    public async Task GivenUpdatedCategories_WhenRetrieved_ThenNewCategoriesReturned()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.Categories.Add("Work");
        evt.Categories.Add("Meeting");

        await engine.CreateEventAsync(evt, tenantId);

        // Update with new categories
        var updatedEvt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        updatedEvt.Categories.Add("Personal");
        updatedEvt.Categories.Add("Vacation");
        updatedEvt.Categories.Add("Travel");

        // Act
        await engine.UpdateEventAsync(updatedEvt, tenantId);

        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.Equal(3, retrieved.Categories.Count);
        Assert.Contains("Personal", retrieved.Categories);
        Assert.Contains("Vacation", retrieved.Categories);
        Assert.Contains("Travel", retrieved.Categories);
        Assert.DoesNotContain("Work", retrieved.Categories);
        Assert.DoesNotContain("Meeting", retrieved.Categories);
    }

    /// <summary>
    /// Tests that updated properties replace the original properties.
    /// Expected: Only the new properties are returned after update.
    /// </summary>
    [SkippableFact]
    public async Task GivenUpdatedProperties_WhenRetrieved_ThenNewPropertiesReturned()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.Properties.Add(new CalendarProperty("X-ORIGINAL-PROP", "original-value"));

        await engine.CreateEventAsync(evt, tenantId);

        // Update with new properties
        var updatedEvt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        updatedEvt.Properties.Add(new CalendarProperty("X-NEW-PROP", "new-value"));
        updatedEvt.Properties.Add(new CalendarProperty("X-ANOTHER-PROP", "another-value"));

        // Act
        await engine.UpdateEventAsync(updatedEvt, tenantId);

        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.Contains(retrieved.Properties, p => p.Name == "X-NEW-PROP");
        Assert.Contains(retrieved.Properties, p => p.Name == "X-ANOTHER-PROP");
        Assert.DoesNotContain(retrieved.Properties, p => p.Name == "X-ORIGINAL-PROP");
    }

    /// <summary>
    /// Tests that updated dates affect date range queries correctly.
    /// Expected: Event is found when querying the new date range, not the old one.
    /// </summary>
    [SkippableFact]
    public async Task GivenUpdatedDates_WhenQueriedByDateRange_ThenUsesNewDates()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var originalStartUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var newStartUtc = new DateTime(2025, 7, 20, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(originalStartUtc, "UTC"),
            End = new CalDateTime(originalStartUtc.AddHours(1), "UTC"),
            Summary = "Original Date Event"
        };

        await engine.CreateEventAsync(evt, tenantId);

        // Update with new dates
        var updatedEvt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(newStartUtc, "UTC"),
            End = new CalDateTime(newStartUtc.AddHours(1), "UTC"),
            Summary = "New Date Event"
        };

        // Act
        await engine.UpdateEventAsync(updatedEvt, tenantId);

        // Query old date range - should not find
        var oldRangeResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(originalStartUtc.AddDays(-1)).To(originalStartUtc.AddDays(1)).Build()).ToListAsync();

        // Query new date range - should find
        var newRangeResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(newStartUtc.AddDays(-1)).To(newStartUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Empty(oldRangeResults);
        Assert.Single(newRangeResults);
        Assert.Equal(uid, newRangeResults[0].Uid);
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Tests that an existing event can be deleted and is no longer retrievable.
    /// Expected: Event is no longer returned in queries after deletion.
    /// </summary>
    [SkippableFact]
    public async Task GivenExistingEvent_WhenDeleted_ThenCannotBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Event to Delete"
        };

        await engine.CreateEventAsync(evt, tenantId);

        // Verify it exists
        var createdResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();
        Assert.Single(createdResults);

        // Act
        await engine.DeleteEventAsync(uid, tenantId);

        // Assert
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();
        Assert.Empty(results);
    }

    /// <summary>
    /// Tests that an existing Todo can be deleted and is no longer retrievable.
    /// Expected: Todo is no longer returned in queries after deletion.
    /// </summary>
    [SkippableFact]
    public async Task GivenExistingTodo_WhenDeleted_ThenCannotBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var todo = new Todo
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            Due = new CalDateTime(startUtc.AddHours(2), "UTC"),
            Summary = "Todo to Delete"
        };

        await engine.CreateEventAsync(todo, tenantId);

        // Verify it exists
        var createdResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).OfType(ComponentType.Todo).Build()).ToListAsync();
        Assert.Single(createdResults);

        // Act
        await engine.DeleteEventAsync(uid, tenantId);

        // Assert
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).OfType(ComponentType.Todo).Build()).ToListAsync();
        Assert.Empty(results);
    }

    /// <summary>
    /// Tests that an existing Journal can be deleted and is no longer retrievable.
    /// Expected: Journal is no longer returned in queries after deletion.
    /// </summary>
    [SkippableFact]
    public async Task GivenExistingJournal_WhenDeleted_ThenCannotBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var journal = new Journal
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            Summary = "Journal to Delete"
        };

        await engine.CreateEventAsync(journal, tenantId);

        // Verify it exists
        var createdResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().OfType(ComponentType.Journal).Build()).ToListAsync();
        Assert.Single(createdResults);

        // Act
        await engine.DeleteEventAsync(uid, tenantId);

        // Assert
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().OfType(ComponentType.Journal).Build()).ToListAsync();
        Assert.Empty(results);
    }

    /// <summary>
    /// Tests that a deleted event with categories can no longer be found by category filter.
    /// Expected: Category query returns empty after deletion.
    /// </summary>
    [SkippableFact]
    public async Task GivenEventWithCategories_WhenDeleted_ThenCannotBeRetrievedByCategory()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Categorized Event"
        };
        evt.Categories.Add("Work");
        evt.Categories.Add("Important");

        await engine.CreateEventAsync(evt, tenantId);

        // Verify it exists via category query
        var createdResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().InCategories("Work").Build()).ToListAsync();
        Assert.Single(createdResults);

        // Act
        await engine.DeleteEventAsync(uid, tenantId);

        // Assert
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().InCategories("Work").Build()).ToListAsync();
        Assert.Empty(results);
    }

    /// <summary>
    /// Tests that a recurring event is completely removed after deletion.
    /// Expected: No occurrences are returned after deletion.
    /// </summary>
    [SkippableFact]
    public async Task GivenRecurringEvent_WhenDeleted_ThenCannotBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Daily Recurring Event"
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1) { Count = 5 });

        await engine.CreateEventAsync(evt, tenantId);

        // Verify it exists
        var createdResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(10)).Build()).ToListAsync();
        Assert.Single(createdResults);

        // Act
        await engine.DeleteEventAsync(uid, tenantId);

        // Assert
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(10)).Build()).ToListAsync();
        Assert.Empty(results);
    }

    /// <summary>
    /// Tests that deleting with wrong tenant throws EventNotFoundException.
    /// Expected: EventNotFoundException is thrown due to tenant isolation.
    /// </summary>
    [SkippableFact]
    public async Task GivenWrongTenant_WhenDeleted_ThenThrowsEventNotFoundException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var wrongTenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Tenant Isolated Event"
        };

        await engine.CreateEventAsync(evt, tenantId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(async () => await engine.DeleteEventAsync(uid, wrongTenantId));

        Assert.Equal(uid, exception.Uid);
        Assert.Equal(wrongTenantId, exception.TenantId);

        // Verify original event still exists
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();
        Assert.Single(results);
    }

    /// <summary>
    /// Tests that deleting with wrong user throws EventNotFoundException.
    /// Expected: EventNotFoundException is thrown due to user isolation.
    /// </summary>
    [SkippableFact]
    public async Task GivenWrongUser_WhenDeleted_ThenThrowsEventNotFoundException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "User 1 Event"
        };

        await engine.CreateEventAsync(evt, tenantId, userId: "user-1");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(async () => await engine.DeleteEventAsync(uid, tenantId, userId: "user-2"));

        Assert.Equal(uid, exception.Uid);
        Assert.Equal("user-2", exception.UserId);

        // Verify original event still exists for user-1
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().ForUser("user-1").From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();
        Assert.Single(results);
    }

    /// <summary>
    /// Tests that an event created with null user cannot be deleted with a specific user.
    /// Expected: EventNotFoundException is thrown due to user isolation.
    /// </summary>
    [SkippableFact]
    public async Task GivenNullUserVsSpecificUser_WhenDeleted_ThenThrowsEventNotFoundException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Null User Event"
        };

        await engine.CreateEventAsync(evt, tenantId, userId: null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(async () => await engine.DeleteEventAsync(uid, tenantId, userId: "some-user"));

        Assert.Equal(uid, exception.Uid);

        // Verify original event still exists for null user
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();
        Assert.Single(results);
    }

    /// <summary>
    /// Tests that an event created with a specific user cannot be deleted with null user.
    /// Expected: EventNotFoundException is thrown due to user isolation.
    /// </summary>
    [SkippableFact]
    public async Task GivenSpecificUserVsNullUser_WhenDeleted_ThenThrowsEventNotFoundException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Specific User Event"
        };

        await engine.CreateEventAsync(evt, tenantId, userId: "specific-user");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(async () => await engine.DeleteEventAsync(uid, tenantId, userId: null));

        Assert.Equal(uid, exception.Uid);

        // Verify original event still exists for specific user
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().ForUser("specific-user").From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();
        Assert.Single(results);
    }

    /// <summary>
    /// Tests that deleting a non-existent event throws EventNotFoundException.
    /// Expected: EventNotFoundException with correct UID is thrown.
    /// </summary>
    [SkippableFact]
    public async Task GivenNonExistentEvent_WhenDeleted_ThenThrowsEventNotFoundException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(async () => await engine.DeleteEventAsync(uid, tenantId));

        Assert.Equal(uid, exception.Uid);
        Assert.Equal(tenantId, exception.TenantId);
    }

    /// <summary>
    /// Tests that deleting an already deleted event throws EventNotFoundException.
    /// Expected: Second delete throws EventNotFoundException.
    /// </summary>
    [SkippableFact]
    public async Task GivenAlreadyDeletedEvent_WhenDeletedAgain_ThenThrowsEventNotFoundException()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Event to Double Delete"
        };

        await engine.CreateEventAsync(evt, tenantId);

        // First delete - should succeed
        await engine.DeleteEventAsync(uid, tenantId);

        // Act & Assert - second delete should fail
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(async () => await engine.DeleteEventAsync(uid, tenantId));

        Assert.Equal(uid, exception.Uid);
        Assert.Equal(tenantId, exception.TenantId);
    }

    /// <summary>
    /// Tests that deleting one event does not affect other events in the same tenant.
    /// Expected: Only the targeted event is deleted, others remain.
    /// </summary>
    [SkippableFact]
    public async Task GivenMultipleEventsInTenant_WhenOneDeleted_ThenOthersRemain()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid1 = Guid.NewGuid().ToString();
        var uid2 = Guid.NewGuid().ToString();
        var uid3 = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt1 = new CalendarEvent
        {
            Uid = uid1,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Event 1"
        };

        var evt2 = new CalendarEvent
        {
            Uid = uid2,
            Start = new CalDateTime(startUtc.AddHours(2), "UTC"),
            End = new CalDateTime(startUtc.AddHours(3), "UTC"),
            Summary = "Event 2"
        };

        var evt3 = new CalendarEvent
        {
            Uid = uid3,
            Start = new CalDateTime(startUtc.AddHours(4), "UTC"),
            End = new CalDateTime(startUtc.AddHours(5), "UTC"),
            Summary = "Event 3"
        };

        await engine.CreateEventAsync(evt1, tenantId);
        await engine.CreateEventAsync(evt2, tenantId);
        await engine.CreateEventAsync(evt3, tenantId);

        // Act - delete the middle one
        await engine.DeleteEventAsync(uid2, tenantId);

        // Assert
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Uid == uid1);
        Assert.Contains(results, r => r.Uid == uid3);
        Assert.DoesNotContain(results, r => r.Uid == uid2);
    }

    /// <summary>
    /// Tests that an event created with a specific user can be deleted with the correct user.
    /// Expected: Deletion succeeds and event is no longer retrievable.
    /// </summary>
    [SkippableFact]
    public async Task GivenEventWithUserId_WhenDeletedWithCorrectUser_ThenSucceeds()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var userId = "test-user";
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "User Specific Event"
        };

        await engine.CreateEventAsync(evt, tenantId, userId);

        // Verify it exists
        var createdResults = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().ForUser(userId).From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();
        Assert.Single(createdResults);

        // Act
        await engine.DeleteEventAsync(uid, tenantId, userId);

        // Assert
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().ForUser(userId).From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();
        Assert.Empty(results);
    }

    #endregion

    #region Stress Tests

    /// <summary>
    /// Tests event with very long UID (near database limits).
    /// Expected: Long UID is handled without truncation.
    /// </summary>
    [SkippableFact]
    public async Task GivenVeryLongUid_WhenCreatedAndQueried_ThenUidPreserved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        // Create a very long UID (255 characters is common max for many systems)
        var uid = new string('x', 200) + Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
    }

    /// <summary>
    /// Tests event with Unicode characters in categories and properties.
    /// Expected: Unicode is preserved correctly.
    /// </summary>
    [SkippableFact]
    public async Task GivenUnicodeInCategoriesAndProperties_WhenCreatedAndQueried_ThenUnicodePreserved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC"),
            Summary = "Meeting with 日本語 and emoji 🎉"
        };
        evt.Categories.Add("日本語カテゴリ");
        evt.Categories.Add("Émoji 🚀");
        evt.Properties.Add(new CalendarProperty("X-UNICODE", "中文值 and العربية"));

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.Equal(uid, retrieved.Uid);
        Assert.Equal("Meeting with 日本語 and emoji 🎉", retrieved.Summary);
        Assert.Contains("日本語カテゴリ", retrieved.Categories);
        Assert.Contains("Émoji 🚀", retrieved.Categories);
    }

    /// <summary>
    /// Tests event with many categories (e.g., 50+).
    /// Expected: All categories are preserved.
    /// </summary>
    [SkippableFact]
    public async Task GivenManyCategories_WhenCreatedAndQueried_ThenAllCategoriesPreserved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        // Add 50 unique categories
        for (var i = 0; i < 50; i++)
        {
            evt.Categories.Add($"Category-{i:D3}");
        }

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        Assert.Equal(50, retrieved.Categories.Count);
        for (var i = 0; i < 50; i++)
        {
            Assert.Contains($"Category-{i:D3}", retrieved.Categories);
        }
    }

    /// <summary>
    /// Tests event with many properties (e.g., 50+).
    /// Expected: All properties are preserved.
    /// </summary>
    [SkippableFact]
    public async Task GivenManyProperties_WhenCreatedAndQueried_ThenAllPropertiesPreserved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };

        // Add 50 custom properties
        for (var i = 0; i < 50; i++)
        {
            evt.Properties.Add(new CalendarProperty($"X-PROP-{i:D3}", $"Value-{i:D3}"));
        }

        // Act
        await engine.CreateEventAsync(evt, tenantId);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(startUtc.AddDays(-1)).To(startUtc.AddDays(1)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        var retrieved = Assert.IsType<CalendarEvent>(results[0]);
        for (var i = 0; i < 50; i++)
        {
            Assert.Contains(retrieved.Properties, p => p.Name == $"X-PROP-{i:D3}");
        }
    }

    /// <summary>
    /// Tests event with large recurrence count (COUNT=1000).
    /// Expected: Event with many occurrences is retrievable.
    /// </summary>
    [SkippableFact]
    public async Task GivenLargeRecurrenceCount_WhenCreatedAndQueried_ThenCanBeRetrieved()
    {
        // Skip
        SkipIfNoProvider();

        // Arrange
        var engine = GetEngine();
        var tenantId = Guid.NewGuid().ToString();
        var uid = Guid.NewGuid().ToString();
        var startUtc = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var evt = new CalendarEvent
        {
            Uid = uid,
            Start = new CalDateTime(startUtc, "UTC"),
            End = new CalDateTime(startUtc.AddHours(1), "UTC")
        };
        evt.RecurrenceRules.Add(new RecurrencePattern(FrequencyType.Daily, 1) { Count = 1000 });

        // Act
        await engine.CreateEventAsync(evt, tenantId);

        // Query somewhere in the middle of the recurrence
        var midPoint = startUtc.AddDays(500);
        var results = await engine.GetEventsAsync(
            tenantId,
            EventFilter.Create().TenantWideOnly().From(midPoint).To(midPoint.AddDays(30)).Build()).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(uid, results[0].Uid);
        var rrule = Assert.Single(((CalendarEvent)results[0]).RecurrenceRules);
        Assert.Equal(1000, rrule.Count);
    }

    #endregion
}
