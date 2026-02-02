![RecurringThings](assets/github-header-banner.png)

[![CI](https://github.com/ChuckNovice/RecurringThings/actions/workflows/ci.yml/badge.svg)](https://github.com/ChuckNovice/RecurringThings/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/RecurringThings.svg)](https://www.nuget.org/packages/RecurringThings/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

Built on [iCal.Net](https://github.com/rianjs/ical.net) for RFC 5545 recurrence rules, RecurringThings adds a persistence layer with multi-tenant isolation and efficient date range queries.

## Features

- **Multi-tenant isolation** - TenantId and resource path scoping
- **Efficient date range queries** - Query only the events you need
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
    builder.UseMongoDb((provider, options) =>
    {
        options.ConnectionString = "mongodb://localhost:27017";
        options.DatabaseName = "myapp";
        options.CollectionName = "mycalendar";
    }));

// Or using PostgreSQL
services.AddRecurringThings(builder =>
    builder.UsePostgreSql((provider, options) =>
    {
        options.ConnectionString = "Host=localhost;Database=myapp;Username=user;Password=pass";
    }));
```

The `IServiceProvider` parameter allows you to resolve services registered before `AddRecurringThings`, such as `IOptions<T>` or `IConfiguration`.

Then inject `IRecurrenceEngine` wherever you need it.

## Usage

**Create an event:**

```csharp
var evt = new CalendarEvent
{
    Uid = "meeting-123",
    Summary = "Weekly Standup",
    Start = new CalDateTime(2025, 1, 6, 9, 0, 0, "America/New_York"),
    Duration = TimeSpan.FromMinutes(30),
    RecurrenceRules = [new RecurrencePattern(FrequencyType.Weekly)]
};

await engine.CreateEventAsync(evt, "tenant-1", "user-abc", cancellationToken);
```

**Query events:**

```csharp
var filter = EventFilter.Create()
    .ForUser("user-abc")
    .From(DateTime.UtcNow)
    .To(DateTime.UtcNow.AddMonths(1))
    .Build();

await foreach (var evt in engine.GetEventsAsync("tenant-1", filter).WithCancellation(cancellationToken))
{
    Console.WriteLine(evt.Summary);
}
```

## Supported Databases

| Provider | Documentation |
|----------|---------------|
| MongoDB | [RecurringThings.MongoDB](src/RecurringThings.MongoDB/README.md) |
| PostgreSQL | [RecurringThings.PostgreSQL](src/RecurringThings.PostgreSQL/README.md) |

## License

Apache 2.0 - see [LICENSE](LICENSE)
