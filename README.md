# RecurringThings

[![CI](https://github.com/ChuckNovice/RecurringThings/actions/workflows/ci.yml/badge.svg)](https://github.com/ChuckNovice/RecurringThings/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/RecurringThings.svg)](https://www.nuget.org/packages/RecurringThings/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

A .NET library for managing recurring events with on-demand virtualization. Instead of pre-materializing all future instances, RecurringThings generates occurrences dynamically during queries, enabling efficient storage and flexible manipulation of calendar-like data.

## Features

- **On-demand virtualization**: Generate recurring instances only when queried, not stored
- **Multi-tenant isolation**: Built-in organization and resource path scoping
- **Database flexibility**: Pluggable persistence with MongoDB and PostgreSQL
- **Transaction support**: Integration with Transactional library for ACID operations
- **Time zone correctness**: Proper DST handling using IANA time zones and NodaTime
- **RFC 5545 RRule support**: Full recurrence rule support via Ical.Net

## Installation

RecurringThings is distributed as three NuGet packages:

```bash
# Core library (always required)
dotnet add package RecurringThings

# Choose ONE persistence provider:
dotnet add package RecurringThings.MongoDB     # For MongoDB
dotnet add package RecurringThings.PostgreSQL  # For PostgreSQL
```

## Quick Start

### MongoDB Setup

```csharp
using RecurringThings.Configuration;
using RecurringThings.MongoDB.Configuration;

services.AddRecurringThings(builder =>
    builder.UseMongoDb(options =>
    {
        options.ConnectionString = "mongodb://localhost:27017";
        options.DatabaseName = "myapp";
        options.CollectionName = "recurring_things"; // Optional, default
    }));
```

### PostgreSQL Setup

```csharp
using RecurringThings.Configuration;
using RecurringThings.PostgreSQL.Configuration;

services.AddRecurringThings(builder =>
    builder.UsePostgreSql(options =>
    {
        options.ConnectionString = "Host=localhost;Database=myapp;Username=user;Password=pass";
        options.RunMigrationsOnStartup = true; // Optional, default is true
    }));
```

### Basic Usage

```csharp
public class CalendarService(IRecurrenceEngine engine)
{
    public async Task CreateWeeklyMeetingAsync()
    {
        var recurrence = await engine.CreateRecurrenceAsync(new RecurrenceCreate
        {
            Organization = "tenant1",
            ResourcePath = "user123/calendar",
            Type = "meeting",
            StartTimeUtc = new DateTime(2025, 1, 6, 14, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTimeUtc = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            RRule = "FREQ=WEEKLY;BYDAY=MO;UNTIL=20251231T235959Z",
            TimeZone = "America/New_York",
            Extensions = new Dictionary<string, string>
            {
                ["title"] = "Weekly Team Standup",
                ["location"] = "Conference Room A"
            }
        });
    }

    public async Task GetJanuaryEntriesAsync()
    {
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        await foreach (var entry in engine.GetAsync("tenant1", "user123/calendar", start, end, null))
        {
            Console.WriteLine($"{entry.Type}: {entry.StartTime} - {entry.EndTime}");
        }
    }
}
```

## Usage Examples

### Creating a Recurrence

```csharp
// Daily weekday recurrence (Mon-Fri)
var recurrence = await engine.CreateRecurrenceAsync(new RecurrenceCreate
{
    Organization = "tenant1",
    ResourcePath = "user123/calendar",
    Type = "appointment",
    StartTimeUtc = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc),
    Duration = TimeSpan.FromHours(1),
    RecurrenceEndTimeUtc = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
    RRule = "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20251231T235959Z",
    TimeZone = "America/New_York"
});
```

### Creating a Standalone Occurrence

```csharp
// Single non-recurring event
var occurrence = await engine.CreateOccurrenceAsync(new OccurrenceCreate
{
    Organization = "tenant1",
    ResourcePath = "user123/calendar",
    Type = "meeting",
    StartTimeUtc = new DateTime(2025, 3, 15, 14, 0, 0, DateTimeKind.Utc),
    Duration = TimeSpan.FromMinutes(30),
    TimeZone = "America/New_York",
    Extensions = new Dictionary<string, string>
    {
        ["title"] = "One-time Client Meeting"
    }
});
```

### Querying Entries

Results are streamed via `IAsyncEnumerable` for efficient memory usage:

```csharp
// Get all entries in a date range
await foreach (var entry in engine.GetAsync(
    organization: "tenant1",
    resourcePath: "user123/calendar",
    startUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    endUtc: new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
    types: null)) // null = all types
{
    // entry.RecurrenceId is set for recurrence patterns
    // entry.OccurrenceId is set for standalone occurrences
    // entry.RecurrenceOccurrenceDetails is set for virtualized occurrences
    Console.WriteLine($"{entry.StartTime}: {entry.Type}");
}

// Filter by specific types
await foreach (var entry in engine.GetAsync(
    "tenant1", "user123/calendar", start, end,
    types: ["appointment", "meeting"]))
{
    // Only appointments and meetings
}
```

### Updating Entries

Update behavior depends on entry type:

```csharp
// Get an entry
var entries = await engine.GetAsync(org, path, start, end, null).ToListAsync();
var entry = entries.First();

// Update a recurrence (only Duration and Extensions can be modified)
if (entry.RecurrenceId.HasValue && entry.RecurrenceOccurrenceDetails == null)
{
    entry.Duration = TimeSpan.FromMinutes(90);
    entry.Extensions = new Dictionary<string, string> { ["updated"] = "true" };
    var updated = await engine.UpdateAsync(entry);
}

// Update a standalone occurrence (StartTime, Duration, Extensions can be modified)
if (entry.OccurrenceId.HasValue)
{
    entry.StartTime = entry.StartTime.AddHours(1);
    entry.Duration = TimeSpan.FromMinutes(45);
    var updated = await engine.UpdateAsync(entry);
    // EndTime is automatically recomputed
}

// Update a virtualized occurrence (creates or updates an override)
if (entry.RecurrenceOccurrenceDetails != null)
{
    entry.Duration = TimeSpan.FromMinutes(45);
    var updated = await engine.UpdateAsync(entry);
    // Original values are preserved in RecurrenceOccurrenceDetails.Original
}
```

### Deleting Entries

Delete behavior depends on entry type:

```csharp
// Delete a recurrence - deletes entire series including all exceptions and overrides
await engine.DeleteAsync(recurrenceEntry);

// Delete a standalone occurrence - direct delete
await engine.DeleteAsync(standaloneEntry);

// Delete a virtualized occurrence - creates an exception (occurrence won't appear in future queries)
await engine.DeleteAsync(virtualizedEntry);
```

### Restoring Overridden Occurrences

If a virtualized occurrence has been modified (has an override), you can restore it to its original state:

```csharp
// Only works for virtualized occurrences that have an override
if (entry.OverrideId.HasValue)
{
    await engine.RestoreAsync(entry);
    // The override is deleted; the occurrence reverts to its original recurrence values
}
```

### Using Transactions

RecurringThings integrates with the Transactional library for ACID operations:

```csharp
// With MongoDB
var transactionManager = serviceProvider.GetRequiredService<IMongoTransactionManager>();
await using var context = await transactionManager.BeginTransactionAsync();

try
{
    await engine.CreateRecurrenceAsync(request1, context);
    await engine.CreateOccurrenceAsync(request2, context);
    await context.CommitAsync();
}
catch
{
    await context.RollbackAsync();
    throw;
}
```

## Domain Model

### Core Entities

| Entity | Description |
|--------|-------------|
| **Recurrence** | Stores RRule pattern, time window, timezone, and duration |
| **Occurrence** | Standalone non-repeating events |
| **OccurrenceException** | Cancels a virtualized recurrence instance |
| **OccurrenceOverride** | Replaces a virtualized instance with customized values |
| **CalendarEntry** | Unified query result abstraction for all entry types |

### CalendarEntry Type Identification

```csharp
// Recurrence pattern (the series definition itself)
if (entry.RecurrenceId.HasValue && entry.RecurrenceOccurrenceDetails == null)
{
    // This is the recurrence pattern, not a virtualized occurrence
    var rrule = entry.RecurrenceDetails?.RRule;
}

// Standalone occurrence
if (entry.OccurrenceId.HasValue)
{
    // Non-recurring single event
}

// Virtualized occurrence from a recurrence
if (entry.RecurrenceOccurrenceDetails != null)
{
    // Generated from a recurrence pattern
    var recurrenceId = entry.RecurrenceOccurrenceDetails.RecurrenceId;

    if (entry.OverrideId.HasValue)
    {
        // Has been modified; original values in entry.RecurrenceOccurrenceDetails.Original
    }
}
```

## Immutability Rules

### Recurrence (Pattern)

| Field | Mutable |
|-------|---------|
| Organization | No |
| ResourcePath | No |
| Type | No |
| TimeZone | No |
| StartTime | No |
| RRule | No |
| RecurrenceEndTime | No |
| **Duration** | **Yes** |
| **Extensions** | **Yes** |

### Standalone Occurrence

| Field | Mutable |
|-------|---------|
| Organization | No |
| ResourcePath | No |
| Type | No |
| TimeZone | No |
| **StartTime** | **Yes** |
| **Duration** | **Yes** |
| **Extensions** | **Yes** |

Note: When StartTime or Duration is modified, EndTime is automatically recomputed.

### Override (Virtualized Occurrence Modification)

| Field | Mutable |
|-------|---------|
| **StartTime** | **Yes** |
| **Duration** | **Yes** |
| **Extensions** | **Yes** |

## State Transition Rules

### Update Operations

| Entry Type | Has Override | Allowed | Behavior |
|------------|--------------|---------|----------|
| Recurrence | N/A | Yes | Update Duration/Extensions only |
| Standalone | N/A | Yes | Update StartTime/Duration/Extensions, recompute EndTime |
| Virtualized | No | Yes | Create override (denormalize original values) |
| Virtualized | Yes | Yes | Update override, recompute EndTime |

### Delete Operations

| Entry Type | Has Override | Allowed | Behavior |
|------------|--------------|---------|----------|
| Recurrence | N/A | Yes | Delete series + all exceptions/overrides |
| Standalone | N/A | Yes | Direct delete |
| Virtualized | No | Yes | Create exception |
| Virtualized | Yes | Yes | Delete override, create exception at original time |

### Restore Operations

| Entry Type | Has Override | Allowed | Behavior |
|------------|--------------|---------|----------|
| Recurrence | N/A | No | Error |
| Standalone | N/A | No | Error |
| Virtualized | No | No | Error |
| Virtualized | Yes | Yes | Delete override |

## RRule Requirements

### UNTIL Required, COUNT Not Supported

RecurringThings requires RRule to specify UNTIL; COUNT is not supported.

**Rationale**: COUNT prevents efficient query range filtering since the recurrence end time cannot be determined without full virtualization.

```csharp
// Valid - uses UNTIL
RRule = "FREQ=DAILY;UNTIL=20251231T235959Z"

// Invalid - uses COUNT
RRule = "FREQ=DAILY;COUNT=10"  // Throws ArgumentException
```

### UNTIL Must Be UTC

The UNTIL value must be in UTC (Z suffix) to eliminate DST ambiguity.

```csharp
// Valid - UTC time with Z suffix
RRule = "FREQ=WEEKLY;BYDAY=MO;UNTIL=20251231T235959Z"

// Invalid - local time without Z suffix
RRule = "FREQ=WEEKLY;BYDAY=MO;UNTIL=20251231T235959"  // Throws ArgumentException
```

### RecurrenceEndTimeUtc Must Match UNTIL

The `RecurrenceEndTimeUtc` field must match the UNTIL value in the RRule.

## Time Zone Handling

RecurringThings uses IANA time zone identifiers (e.g., `America/New_York`) and NodaTime for correct DST handling.

### DST Transitions

The library correctly handles:
- **Spring forward** (2 AM -> 3 AM): Skipped hours don't generate occurrences
- **Fall back** (2 AM -> 1 AM): Ambiguous times resolved consistently

```csharp
var recurrence = await engine.CreateRecurrenceAsync(new RecurrenceCreate
{
    // ...
    TimeZone = "America/New_York",  // IANA identifier required
    RRule = "FREQ=DAILY;UNTIL=20251231T235959Z"
});
```

### Virtualization Flow

1. Query recurrences whose series intersect with query range
2. Convert UTC to local time using IANA time zone
3. Apply RRule using Ical.Net to generate theoretical instances
4. Convert instances back to UTC
5. Apply exceptions (discard) and overrides (replace)
6. Filter by query range

## Multi-Tenancy

### Organization Isolation

All entities require an `Organization` identifier for multi-tenant SaaS applications:

```csharp
var entry = await engine.CreateRecurrenceAsync(new RecurrenceCreate
{
    Organization = "tenant-abc",  // Required, 0-100 characters
    ResourcePath = "user123/calendar",
    // ...
});
```

### Resource Path Scoping

`ResourcePath` enables hierarchical resource organization:

```csharp
// Examples of resource paths
ResourcePath = "user123/calendar"
ResourcePath = "store456/open-hours"
ResourcePath = ""  // Empty is allowed for global scope
```

All queries require both Organization and ResourcePath for data isolation.

## Performance

### Streaming Results

`GetAsync` returns `IAsyncEnumerable<CalendarEntry>` for efficient memory usage:

```csharp
// Results are streamed, not materialized all at once
await foreach (var entry in engine.GetAsync(org, path, start, end, null))
{
    yield return Transform(entry);
}
```

### Parallel Query Execution

The engine executes independent database queries in parallel for better performance.

### Indexing

Both MongoDB and PostgreSQL implementations create appropriate indexes for efficient querying by organization, resource path, type, and time ranges.

## Configuration Rules

- **Exactly one persistence provider required**: Application fails at startup if neither or both providers are configured
- **MongoDB**: `DatabaseName` is required, `CollectionName` is optional
- **PostgreSQL**: Database must exist, schema is auto-created if missing

## Contributing

### Development Setup

```bash
git clone https://github.com/ChuckNovice/RecurringThings.git
cd RecurringThings
dotnet restore
dotnet build
```

### Running Tests

```bash
# Run all unit tests
dotnet test --filter 'Category!=Integration'

# Run integration tests (requires databases)
export MONGODB_CONNECTION_STRING="mongodb://localhost:27017"
export POSTGRES_CONNECTION_STRING="Host=localhost;Database=test;Username=user;Password=pass"
dotnet test --filter 'Category=Integration'
```

### Code Formatting

```bash
# Check formatting
dotnet format --verify-no-changes

# Apply formatting
dotnet format
```

## License

This project is licensed under the Apache 2.0 License - see the [LICENSE](LICENSE) file for details.
