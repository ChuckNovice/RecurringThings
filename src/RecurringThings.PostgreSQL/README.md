# RecurringThings.PostgreSQL

[![NuGet](https://img.shields.io/nuget/v/RecurringThings.PostgreSQL.svg)](https://www.nuget.org/packages/RecurringThings.PostgreSQL/)

PostgreSQL persistence provider for [RecurringThings](../../README.md).

## Installation

```bash
dotnet add package RecurringThings
dotnet add package RecurringThings.PostgreSQL
```

## Configuration

```csharp
using RecurringThings.Configuration;
using RecurringThings.PostgreSQL.Configuration;

services.AddRecurringThings(builder =>
    builder.UsePostgreSql(options =>
    {
        options.ConnectionString = "Host=localhost;Database=myapp;Username=user;Password=pass";
        options.RunMigrationsOnStartup = true;  // Optional, default is true
    }));
```

## Migrations

Migrations run automatically on startup when `RunMigrationsOnStartup = true` (default). The provider uses Entity Framework Core migrations with PostgreSQL advisory locks to ensure safe concurrent migration across multiple application replicas.

To disable automatic migrations:

```csharp
builder.UsePostgreSql(options =>
{
    options.ConnectionString = connectionString;
    options.RunMigrationsOnStartup = false;
});
```

## Indexes

The provider creates indexes for efficient querying:

- `(organization, resource_path, start_time, recurrence_end_time)` on recurrences
- `(organization, resource_path, start_time, end_time)` on occurrences
- `(recurrence_id)` on exceptions and overrides

## Transactions

Use `IPostgresTransactionManager` from the [Transactional](https://github.com/ChuckNovice/transactional) library:

```csharp
using Transactional.PostgreSQL;

public class CalendarService(IRecurrenceEngine engine, IPostgresTransactionManager transactionManager)
{
    public async Task CreateMultipleEntriesAsync()
    {
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
    }
}
```

## Usage Examples

### Basic Setup

```csharp
using Ical.Net.DataTypes;

public class CalendarService(IRecurrenceEngine engine)
{
    public async Task CreateWeeklyMeetingAsync()
    {
        // Build a recurrence pattern using Ical.Net
        var pattern = new RecurrencePattern
        {
            Frequency = FrequencyType.Weekly,
            Until = new CalDateTime(new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Local).ToUniversalTime())
        };
        pattern.ByDay.Add(new WeekDay(DayOfWeek.Monday));

        // RecurrenceEndTime is automatically extracted from the RRule UNTIL clause
        var recurrence = await engine.CreateRecurrenceAsync(
            organization: "tenant1",
            resourcePath: "user123/calendar",
            type: "meeting",
            startTime: DateTime.Now,
            duration: TimeSpan.FromHours(1),
            rrule: pattern.ToString(),
            timeZone: "America/New_York",
            extensions: new Dictionary<string, string>
            {
                ["title"] = "Weekly Team Standup",
                ["location"] = "Conference Room A"
            });
    }

    public async Task GetJanuaryEntriesAsync()
    {
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        await foreach (var entry in engine.GetOccurrencesAsync("tenant1", "user123/calendar", start, end, null))
        {
            Console.WriteLine($"{entry.Type}: {entry.StartTime} - {entry.EndTime} ({entry.EntryType})");
        }
    }
}
```

### Querying with Type Filter

```csharp
// Get only appointments and meetings
await foreach (var entry in engine.GetOccurrencesAsync(
    "tenant1", "user123/calendar", start, end,
    types: ["appointment", "meeting"]))
{
    // Process filtered entries
}
```

### Updating Entries

```csharp
// Update a standalone occurrence
var entries = await engine.GetOccurrencesAsync(org, path, start, end, null).ToListAsync();
var entry = entries.First(e => e.EntryType == CalendarEntryType.Standalone);

entry.StartTime = entry.StartTime.AddHours(1);
entry.Duration = TimeSpan.FromMinutes(45);
var updated = await engine.UpdateOccurrenceAsync(entry);
// EndTime is automatically recomputed

// Update a virtualized occurrence (creates an override)
var virtualizedEntry = entries.First(e => e.EntryType == CalendarEntryType.Virtualized);
virtualizedEntry.Duration = TimeSpan.FromMinutes(45);
var overridden = await engine.UpdateOccurrenceAsync(virtualizedEntry);
// Original values preserved in entry.Original
```

### Deleting Entries

```csharp
// Delete entire recurrence series (cascade deletes exceptions/overrides)
await engine.DeleteRecurrenceAsync(org, path, recurrenceId);

// Delete a virtualized occurrence (creates an exception)
await engine.DeleteOccurrenceAsync(virtualizedEntry);

// Restore an overridden occurrence to original state
if (entry.IsOverridden)
{
    await engine.RestoreAsync(entry);
}
```

## Integration Tests

Set the environment variable before running integration tests:

```bash
export POSTGRES_CONNECTION_STRING="Host=localhost;Database=test;Username=user;Password=pass"
dotnet test --filter 'Category=Integration'
```

## Limitations

- Database must exist before running the application (schema is auto-created)
- DateTime values can be UTC or Local (`DateTimeKind.Unspecified` is not allowed)
- RRule must use UNTIL (COUNT is not supported)
- UNTIL must have UTC suffix (Z)
