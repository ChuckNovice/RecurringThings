# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RecurringThings is a .NET 10 library for managing recurring events with on-demand virtualization. It provides three NuGet packages:
- **RecurringThings** (core) - Virtualization engine and abstractions
- **RecurringThings.MongoDB** - MongoDB persistence
- **RecurringThings.PostgreSQL** - PostgreSQL persistence

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

# Check code formatting
dotnet format --verify-no-changes

# Apply code formatting
dotnet format

# Pack NuGet packages
dotnet pack -c Release
```

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
- **NodaTime** - Timezone handling
- **Ical.Net** - RRule parsing
- **Transactional** library - Transaction context abstractions

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

### Testing
- Framework: xUnit
- Mocking: Moq
- Target: 90%+ coverage
- Integration tests require `MONGODB_CONNECTION_STRING` and `POSTGRES_CONNECTION_STRING` environment variables

## Multi-Tenancy
All entities require:
- `Organization` - Tenant identifier (0-100 chars, empty allowed)
- `ResourcePath` - Hierarchical scope like `user123/calendar` (0-100 chars, empty allowed)

Exceptions and overrides must belong to the same Organization and ResourcePath as their parent recurrence.
