# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Critical Rules

1. **Documentation must stay current** - All code changes must be reflected in both XML documentation and the appropriate README file (see "Project Structure and Documentation" section). Stale documentation is unacceptable.
2. **Run `dotnet format` before every commit** - Code formatting must be verified before committing.
3. **No commercial libraries** - Do not use libraries that have become commercial or have restrictive licensing.
4. **Convention over attributes** - Configure conventions once and reuse them rather than using attributes on multiple properties (e.g., use camelCase conventions for JSON serialization, BSON element naming for MongoDB).
5. **Use FluentValidation** - Prefer FluentValidation for defining validation rules and validating complex objects rather than manually writing validation code in extension methods.

## Project Overview

RecurringThings is a .NET 10 library for managing recurring events with on-demand virtualization. It provides three NuGet packages:
- **RecurringThings** (core) - Virtualization engine and abstractions
- **RecurringThings.MongoDB** - MongoDB persistence
- **RecurringThings.PostgreSQL** - PostgreSQL persistence

## Project Structure and Documentation

**IMPORTANT:** The three projects in this repository should be treated as **completely separate packages**, even though they share a repository and are published together.

### Separate Project Principle
- **RecurringThings** (core) - Database-agnostic engine with repository abstractions
- **RecurringThings.MongoDB** - Standalone MongoDB implementation (depends on core)
- **RecurringThings.PostgreSQL** - Standalone PostgreSQL implementation (depends on core)

Each database provider is independent and users typically install only one alongside the core package.

### Documentation Structure
Each project has its own README file:

| Project | README Location | Content Focus |
|---------|-----------------|---------------|
| Core | `README.md` (repository root) | Engine features, domain model, virtualization concepts, supported databases (with links to provider READMEs) |
| MongoDB | `src/RecurringThings.MongoDB/README.md` | MongoDB-specific setup, configuration, schema, indexes, transactions |
| PostgreSQL | `src/RecurringThings.PostgreSQL/README.md` | PostgreSQL-specific setup, configuration, schema, migrations, transactions |

### Documentation Rules
1. **Main README** (`README.md`) should:
   - Focus on core engine capabilities and concepts
   - Explain the virtualization flow and domain model
   - List supported databases with links to their respective README files
   - NOT contain database-specific configuration or setup details

2. **Provider READMEs** should:
   - Be self-contained documentation for that provider
   - Include installation, configuration, and usage examples
   - Document schema/collection structure
   - Explain provider-specific transaction handling
   - Include any provider-specific limitations or considerations

## Build and Test Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run unit tests only (exclude integration tests)
dotnet test --filter 'Category!=Integration'

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Check code formatting (run before every commit!)
dotnet format --verify-no-changes

# Apply code formatting
dotnet format

# Pack NuGet packages
dotnet pack -c Release
```

### Pre-Commit Checklist
1. Run `dotnet format` to fix formatting
2. Run `dotnet build` to verify compilation
3. Run `dotnet test` to verify all tests pass
4. Update XML documentation for any changed public APIs
5. Update the appropriate README file(s) if functionality changed:
   - Core engine changes → `README.md`
   - MongoDB changes → `src/RecurringThings.MongoDB/README.md`
   - PostgreSQL changes → `src/RecurringThings.PostgreSQL/README.md`

## Architecture

### Domain Model
- **Recurrence** - Stores RRule pattern, time window, and timezone (no EndTime stored)
- **Occurrence** - Standalone non-repeating events
- **OccurrenceException** - Cancels a virtualized recurrence instance
- **OccurrenceOverride** - Replaces a virtualized instance with customized values
- **CalendarEntry** - Unified query result abstraction for all entry types

### Virtualization Flow
1. Query recurrences intersecting with date range (using StartTime and RecurrenceEndTime)
2. Convert UTC to local time using IANA timezone
3. Apply RRule via Ical.Net to generate instances
4. Convert back to UTC
5. Apply exceptions (discard) and overrides (replace)
6. Merge with standalone occurrences

### Key Dependencies
- **NodaTime** ([https://github.com/nodatime/nodatime](https://github.com/nodatime/nodatime)) - Timezone handling with IANA timezone support
- **Ical.Net** ([https://github.com/rianjs/ical.net](https://github.com/rianjs/ical.net)) - RFC 5545 RRule parsing and evaluation
- **Transactional** ([https://github.com/ChuckNovice/transactional](https://github.com/ChuckNovice/transactional)) - Transaction context abstractions for repository pattern
- **FluentValidation** ([https://github.com/FluentValidation/FluentValidation](https://github.com/FluentValidation/FluentValidation)) - Validation rules for complex objects (preferred over manual validation)
- **MongoDB.Driver** ([https://github.com/mongodb/mongo-csharp-driver](https://github.com/mongodb/mongo-csharp-driver)) - MongoDB persistence
- **Entity Framework Core** ([https://github.com/dotnet/efcore](https://github.com/dotnet/efcore)) - PostgreSQL persistence (preferred over raw Npgsql)
- **xUnit** ([https://github.com/xunit/xunit](https://github.com/xunit/xunit)) - Testing framework
- **Moq** ([https://github.com/devlooped/moq](https://github.com/devlooped/moq)) - Mocking framework for tests
- Use built-in `Assert` from xUnit for assertions (do NOT use FluentAssertions - it became commercial)

## Code Conventions

### DateTime Handling
- All DateTime fields stored in UTC
- Timezones use IANA format (`America/New_York`, not `Central Standard Time`)
- RRule UNTIL must be in UTC (Z suffix required)
- COUNT in RRule is not supported; use UNTIL only

### Immutability Rules
Recurrences: Only `Duration` and `Extensions` are mutable after creation.
Standalone Occurrences: `StartTime`, `Duration`, and `Extensions` are mutable (EndTime recomputed).
Overrides: `StartTime`, `Duration`, and `Extensions` are mutable (EndTime recomputed).

### C# Style (from .editorconfig)
- File-scoped namespaces required
- Using directives inside namespace
- Use collection expressions `[]` instead of `Array.Empty` or `new[]`
- Prefer primary constructors
- Write XML documentation (`<summary>`, `<param>`, `<returns>`, `<exception>`) on all public classes, methods, and properties

### Serialization Conventions
- Configure naming conventions globally (e.g., camelCase for JSON) rather than using attributes on each property
- For MongoDB: Use conventions or class maps instead of `[BsonElement]` attributes
- For JSON: Use `JsonSerializerOptions` with `PropertyNamingPolicy` instead of `[JsonPropertyName]` attributes

### Testing
- Target: 90%+ coverage
- Integration tests require `MONGODB_CONNECTION_STRING` and `POSTGRES_CONNECTION_STRING` environment variables
- Use xUnit with built-in `Assert` class for assertions
- Use Moq for mocking (see Key Dependencies for links)

## Multi-Tenancy
All entities require:
- `Organization` - Tenant identifier (0-100 chars, empty allowed)
- `ResourcePath` - Hierarchical scope like `user123/calendar` (0-100 chars, empty allowed)

Exceptions and overrides must belong to the same Organization and ResourcePath as their parent recurrence.
