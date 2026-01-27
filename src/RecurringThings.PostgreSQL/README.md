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
public class CalendarService(IRecurrenceEngine engine)
{
    public async Task CreateWeeklyMeetingAsync()
    {
        // RecurrenceEndTime is automatically extracted from the RRule UNTIL clause
        var recurrence = await engine.CreateRecurrenceAsync(new RecurrenceCreate
        {
            Organization = "tenant1",
            ResourcePath = "user123/calendar",
            Type = "meeting",
            StartTime = new DateTime(2025, 1, 6, 14, 0, 0, DateTimeKind.Utc), // Or DateTime.Now for local time
            Duration = TimeSpan.FromHours(1),
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

### Querying with Type Filter

```csharp
// Get only appointments and meetings
await foreach (var entry in engine.GetAsync(
    "tenant1", "user123/calendar", start, end,
    types: ["appointment", "meeting"]))
{
    // Process filtered entries
}
```

### Updating Entries

```csharp
// Update a standalone occurrence
var entries = await engine.GetAsync(org, path, start, end, null).ToListAsync();
var entry = entries.First(e => e.OccurrenceId.HasValue);

entry.StartTime = entry.StartTime.AddHours(1);
entry.Duration = TimeSpan.FromMinutes(45);
var updated = await engine.UpdateAsync(entry);
// EndTime is automatically recomputed

// Update a virtualized occurrence (creates an override)
var virtualizedEntry = entries.First(e => e.RecurrenceOccurrenceDetails != null);
virtualizedEntry.Duration = TimeSpan.FromMinutes(45);
var overridden = await engine.UpdateAsync(virtualizedEntry);
// Original values preserved in RecurrenceOccurrenceDetails.Original
```

### Deleting Entries

```csharp
// Delete entire recurrence series (cascade deletes exceptions/overrides)
await engine.DeleteAsync(recurrenceEntry);

// Delete a virtualized occurrence (creates an exception)
await engine.DeleteAsync(virtualizedEntry);

// Restore an overridden occurrence to original state
if (entry.OverrideId.HasValue)
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
