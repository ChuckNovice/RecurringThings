# RecurringThings

**RecurringThings** is a minimal demo project that showcases state-of-the-art recurrence and occurrence handling for calendar-based applications. It provides a clean Entity Framework Core model for managing recurring schedules, single occurrences, exceptions, and overrides — ideal for building advanced scheduling systems.

## Migration Script Generation

To generate a new migration script, use the following command:

```bash
dotnet ef migrations add InitialCreate --project RecurringThings.PostgreSQL --startup-project RecurringThings.Tests
```