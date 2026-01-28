![RecurringThings](assets/github-header-banner.png)

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
using Ical.Net.DataTypes;
using RecurringThings;

// Build a recurrence pattern using Ical.Net
var pattern = new RecurrencePattern
{
    Frequency = FrequencyType.Weekly,
    Until = new CalDateTime(new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Local).ToUniversalTime())
};
pattern.ByDay.Add(new WeekDay(DayOfWeek.Monday));

// Create a weekly recurring meeting
// Note: RecurrenceEndTime is automatically extracted from the RRule UNTIL clause
await engine.CreateRecurrenceAsync(
    organization: "tenant1",
    resourcePath: "user123/calendar",
    type: "meeting",
    startTime: DateTime.Now,
    duration: TimeSpan.FromHours(1),
    rrule: pattern.ToString(),
    timeZone: "America/New_York");

// Query occurrences in a date range
await foreach (var entry in engine.GetOccurrencesAsync("tenant1", "user123/calendar", start, end, null))
{
    Console.WriteLine($"{entry.Type}: {entry.StartTime} ({entry.EntryType})");
}

// Query recurrence patterns in a date range
await foreach (var entry in engine.GetRecurrencesAsync("tenant1", "user123/calendar", start, end, null))
{
    Console.WriteLine($"Recurrence: {entry.RecurrenceDetails?.RRule}");
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

## Benchmarking

Run benchmarks locally against MongoDB and/or PostgreSQL:

```bash
# Set connection strings (PowerShell)
$env:MONGODB_CONNECTION_STRING = "mongodb://localhost:27017"
$env:POSTGRES_CONNECTION_STRING = "Host=localhost;Database=postgres;Username=postgres;Password=password"

# Run all benchmarks
dotnet run -c Release --project benchmarks/RecurringThings.Benchmarks

# Run specific benchmark class
dotnet run -c Release --project benchmarks/RecurringThings.Benchmarks -- --filter *QueryBenchmarks*
```

Results are generated in `./BenchmarkResults/` including HTML reports and PNG charts.

## License

Apache 2.0 - see [LICENSE](LICENSE)
