# RecurringThings

[![CI](https://github.com/ChuckNovice/RecurringThings/actions/workflows/ci.yml/badge.svg)](https://github.com/ChuckNovice/RecurringThings/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/RecurringThings.svg)](https://www.nuget.org/packages/RecurringThings/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

A .NET library for managing recurring events with on-demand virtualization. Instead of pre-materializing all future instances, RecurringThings generates occurrences dynamically during queries.

## Features

- **On-demand virtualization** - Generate recurring instances only when queried
- **Multi-tenant isolation** - Built-in organization and resource path scoping
- **Time zone correctness** - Proper DST handling using IANA time zones (NodaTime)
- **RFC 5545 RRule support** - Full recurrence rules via Ical.Net
- **Transaction support** - ACID operations via Transactional library

## Installation

```bash
# Core library (required)
dotnet add package RecurringThings

# Choose ONE persistence provider:
dotnet add package RecurringThings.MongoDB
dotnet add package RecurringThings.PostgreSQL
```

## Setup

Register RecurringThings in your `Program.cs` or `Startup.cs`:

```csharp
// Using MongoDB
services.AddRecurringThings(builder =>
    builder.UseMongoDb(options =>
    {
        options.ConnectionString = "mongodb://localhost:27017";
        options.DatabaseName = "myapp";
    }));

// Or using PostgreSQL
services.AddRecurringThings(builder =>
    builder.UsePostgreSql(options =>
    {
        options.ConnectionString = "Host=localhost;Database=myapp;Username=user;Password=pass";
    }));
```

Then inject `IRecurrenceEngine` wherever you need it:

```csharp
public class CalendarService(IRecurrenceEngine engine)
{
    public async Task CreateMeetingAsync() { /* use engine */ }
}
```

## Quick Example

```csharp
// Create a weekly recurring meeting
await engine.CreateRecurrenceAsync(new RecurrenceCreate
{
    Organization = "tenant1",
    ResourcePath = "user123/calendar",
    Type = "meeting",
    StartTimeUtc = new DateTime(2025, 1, 6, 14, 0, 0, DateTimeKind.Utc),
    Duration = TimeSpan.FromHours(1),
    RecurrenceEndTimeUtc = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
    RRule = "FREQ=WEEKLY;BYDAY=MO;UNTIL=20251231T235959Z",
    TimeZone = "America/New_York"
});

// Query entries in a date range
await foreach (var entry in engine.GetAsync("tenant1", "user123/calendar", start, end, null))
{
    Console.WriteLine($"{entry.Type}: {entry.StartTime}");
}
```

## Domain Model

| Entity | Description |
|--------|-------------|
| **Recurrence** | RRule pattern with time window and timezone |
| **Occurrence** | Standalone non-repeating event |
| **OccurrenceException** | Cancels a virtualized instance |
| **OccurrenceOverride** | Modifies a virtualized instance |
| **CalendarEntry** | Unified query result for all types |

## Supported Databases

| Provider | Documentation |
|----------|---------------|
| MongoDB | [RecurringThings.MongoDB](src/RecurringThings.MongoDB/README.md) |
| PostgreSQL | [RecurringThings.PostgreSQL](src/RecurringThings.PostgreSQL/README.md) |

## License

Apache 2.0 - see [LICENSE](LICENSE)
