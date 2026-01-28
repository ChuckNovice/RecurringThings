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

## Sharding Considerations

All queries issued by this provider filter by `Organization` first, making it an ideal shard key for MongoDB sharded clusters. Architects and DBAs should note this when designing their sharding strategy.

## Integration Tests

Set the environment variable before running integration tests:
```bash
export MONGODB_CONNECTION_STRING="mongodb://localhost:27017"
dotnet test --filter 'Category=Integration'
```