# RecurringThings.MongoDB

[![NuGet](https://img.shields.io/nuget/v/RecurringThings.MongoDB.svg)](https://www.nuget.org/packages/RecurringThings.MongoDB/)

MongoDB persistence provider for [RecurringThings](../../README.md).

## Installation

```bash
dotnet add package RecurringThings
dotnet add package RecurringThings.MongoDB
```

## Configuration

```csharp
using RecurringThings.Configuration;
using RecurringThings.MongoDB.Configuration;

services.AddRecurringThings(builder =>
    builder.UseMongoDb(options =>
    {
        options.ConnectionString = "mongodb://localhost:27017";
        options.DatabaseName = "myapp";                    // Required
        options.CollectionName = "recurring_things";       // Optional, default value
    }));
```

## Indexes

The provider automatically creates indexes for efficient querying:

- `organization + resourcePath + documentType`
- `organization + resourcePath + startTime + recurrenceEndTime` (for recurrences)
- `organization + resourcePath + startTime + endTime` (for occurrences)
- `recurrenceId` (for exceptions and overrides)

## Transactions

Use `IMongoTransactionManager` from the [Transactional](https://github.com/ChuckNovice/transactional) library:

```csharp
using Transactional.MongoDB;

public class CalendarService(IRecurrenceEngine engine, IMongoTransactionManager transactionManager)
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

**Note**: MongoDB transactions require a replica set. Standalone MongoDB instances do not support transactions.

## Usage Examples

### Basic Setup

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
export MONGODB_CONNECTION_STRING="mongodb://localhost:27017"
dotnet test --filter 'Category=Integration'
```

## Limitations

- MongoDB transactions require replica set (not available on standalone instances)
- All DateTime values must be UTC
- RRule must use UNTIL (COUNT is not supported)
- UNTIL must have UTC suffix (Z)
