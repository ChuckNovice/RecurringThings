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
    builder.UsePostgreSql((provider, options) =>
    {
        options.ConnectionString = "Host=localhost;Database=myapp;Username=user;Password=pass";
        options.RunMigrationsOnStartup = true;  // Optional, default is true
    }));
```

The `IServiceProvider` parameter allows you to resolve services registered before `AddRecurringThings`, such as `IOptions<T>` or `IConfiguration`.

## Migrations

Migrations run automatically on startup when `RunMigrationsOnStartup = true` (default). The provider uses Entity Framework Core migrations with PostgreSQL advisory locks to ensure safe concurrent migration across multiple application replicas.

To disable automatic migrations:

```csharp
builder.UsePostgreSql((provider, options) =>
{
    options.ConnectionString = connectionString;
    options.RunMigrationsOnStartup = false;
});
```

## Integration Tests

Set the environment variable before running integration tests:

```bash
export POSTGRES_CONNECTION_STRING="Host=localhost;Database=test;Username=user;Password=pass"
dotnet test --filter 'Category=Integration'
```
