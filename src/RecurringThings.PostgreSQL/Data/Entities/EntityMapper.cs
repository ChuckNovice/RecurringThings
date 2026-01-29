namespace RecurringThings.PostgreSQL.Data.Entities;

using System;
using RecurringThings.Domain;
using RecurringThings.Options;

/// <summary>
/// Maps between domain entities and EF Core entities.
/// </summary>
internal static class EntityMapper
{
    /// <summary>
    /// Maps a <see cref="Recurrence"/> domain entity to a <see cref="RecurrenceEntity"/>.
    /// </summary>
    /// <param name="recurrence">The domain entity.</param>
    /// <returns>The EF Core entity.</returns>
    public static RecurrenceEntity ToEntity(Recurrence recurrence)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        return new RecurrenceEntity
        {
            Id = recurrence.Id,
            Organization = recurrence.Organization,
            ResourcePath = recurrence.ResourcePath,
            Type = recurrence.Type,
            StartTime = recurrence.StartTime,
            Duration = recurrence.Duration,
            RecurrenceEndTime = recurrence.RecurrenceEndTime,
            RRule = recurrence.RRule,
            TimeZone = recurrence.TimeZone,
            Extensions = recurrence.Extensions,
            MonthDayBehavior = SerializeMonthDayBehavior(recurrence.MonthDayBehavior)
        };
    }

    /// <summary>
    /// Maps a <see cref="RecurrenceEntity"/> to a <see cref="Recurrence"/> domain entity.
    /// </summary>
    /// <param name="entity">The EF Core entity.</param>
    /// <returns>The domain entity.</returns>
    public static Recurrence ToDomain(RecurrenceEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Recurrence
        {
            Id = entity.Id,
            Organization = entity.Organization,
            ResourcePath = entity.ResourcePath,
            Type = entity.Type,
            StartTime = DateTime.SpecifyKind(entity.StartTime, DateTimeKind.Utc),
            Duration = entity.Duration,
            RecurrenceEndTime = DateTime.SpecifyKind(entity.RecurrenceEndTime, DateTimeKind.Utc),
            RRule = entity.RRule,
            TimeZone = entity.TimeZone,
            Extensions = entity.Extensions,
            MonthDayBehavior = ParseMonthDayBehavior(entity.MonthDayBehavior)
        };
    }

    /// <summary>
    /// Maps an <see cref="Occurrence"/> domain entity to an <see cref="OccurrenceEntity"/>.
    /// </summary>
    /// <param name="occurrence">The domain entity.</param>
    /// <returns>The EF Core entity.</returns>
    public static OccurrenceEntity ToEntity(Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        return new OccurrenceEntity
        {
            Id = occurrence.Id,
            Organization = occurrence.Organization,
            ResourcePath = occurrence.ResourcePath,
            Type = occurrence.Type,
            StartTime = occurrence.StartTime,
            EndTime = occurrence.EndTime,
            Duration = occurrence.Duration,
            TimeZone = occurrence.TimeZone,
            Extensions = occurrence.Extensions
        };
    }

    /// <summary>
    /// Maps an <see cref="OccurrenceEntity"/> to an <see cref="Occurrence"/> domain entity.
    /// </summary>
    /// <param name="entity">The EF Core entity.</param>
    /// <returns>The domain entity.</returns>
    public static Occurrence ToDomain(OccurrenceEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var occurrence = new Occurrence
        {
            Id = entity.Id,
            Organization = entity.Organization,
            ResourcePath = entity.ResourcePath,
            Type = entity.Type,
            TimeZone = entity.TimeZone,
            Extensions = entity.Extensions
        };

        occurrence.Initialize(
            DateTime.SpecifyKind(entity.StartTime, DateTimeKind.Utc),
            entity.Duration);

        return occurrence;
    }

    /// <summary>
    /// Maps an <see cref="OccurrenceException"/> domain entity to an <see cref="OccurrenceExceptionEntity"/>.
    /// </summary>
    /// <param name="exception">The domain entity.</param>
    /// <returns>The EF Core entity.</returns>
    public static OccurrenceExceptionEntity ToEntity(OccurrenceException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new OccurrenceExceptionEntity
        {
            Id = exception.Id,
            Organization = exception.Organization,
            ResourcePath = exception.ResourcePath,
            Type = exception.Type,
            RecurrenceId = exception.RecurrenceId,
            OriginalTimeUtc = exception.OriginalTimeUtc,
            Extensions = exception.Extensions
        };
    }

    /// <summary>
    /// Maps an <see cref="OccurrenceExceptionEntity"/> to an <see cref="OccurrenceException"/> domain entity.
    /// </summary>
    /// <param name="entity">The EF Core entity.</param>
    /// <returns>The domain entity.</returns>
    public static OccurrenceException ToDomain(OccurrenceExceptionEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new OccurrenceException
        {
            Id = entity.Id,
            Organization = entity.Organization,
            ResourcePath = entity.ResourcePath,
            Type = entity.Type,
            RecurrenceId = entity.RecurrenceId,
            OriginalTimeUtc = DateTime.SpecifyKind(entity.OriginalTimeUtc, DateTimeKind.Utc),
            Extensions = entity.Extensions
        };
    }

    /// <summary>
    /// Maps an <see cref="OccurrenceOverride"/> domain entity to an <see cref="OccurrenceOverrideEntity"/>.
    /// </summary>
    /// <param name="override">The domain entity.</param>
    /// <returns>The EF Core entity.</returns>
    public static OccurrenceOverrideEntity ToEntity(OccurrenceOverride @override)
    {
        ArgumentNullException.ThrowIfNull(@override);

        return new OccurrenceOverrideEntity
        {
            Id = @override.Id,
            Organization = @override.Organization,
            ResourcePath = @override.ResourcePath,
            Type = @override.Type,
            RecurrenceId = @override.RecurrenceId,
            OriginalTimeUtc = @override.OriginalTimeUtc,
            StartTime = @override.StartTime,
            EndTime = @override.EndTime,
            Duration = @override.Duration,
            OriginalDuration = @override.OriginalDuration,
            OriginalExtensions = @override.OriginalExtensions,
            Extensions = @override.Extensions
        };
    }

    /// <summary>
    /// Maps an <see cref="OccurrenceOverrideEntity"/> to an <see cref="OccurrenceOverride"/> domain entity.
    /// </summary>
    /// <param name="entity">The EF Core entity.</param>
    /// <returns>The domain entity.</returns>
    public static OccurrenceOverride ToDomain(OccurrenceOverrideEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var @override = new OccurrenceOverride
        {
            Id = entity.Id,
            Organization = entity.Organization,
            ResourcePath = entity.ResourcePath,
            Type = entity.Type,
            RecurrenceId = entity.RecurrenceId,
            OriginalTimeUtc = DateTime.SpecifyKind(entity.OriginalTimeUtc, DateTimeKind.Utc),
            OriginalDuration = entity.OriginalDuration,
            OriginalExtensions = entity.OriginalExtensions,
            Extensions = entity.Extensions
        };

        @override.Initialize(
            DateTime.SpecifyKind(entity.StartTime, DateTimeKind.Utc),
            entity.Duration);

        return @override;
    }

    /// <summary>
    /// Parses a string value to <see cref="MonthDayOutOfBoundsStrategy"/>.
    /// </summary>
    /// <param name="value">The string value ("skip" or "clamp").</param>
    /// <returns>The parsed strategy, or null if the value is null or empty.</returns>
    private static MonthDayOutOfBoundsStrategy? ParseMonthDayBehavior(string? value)
    {
        return value switch
        {
            "skip" => MonthDayOutOfBoundsStrategy.Skip,
            "clamp" => MonthDayOutOfBoundsStrategy.Clamp,
            _ => null
        };
    }

    /// <summary>
    /// Serializes a <see cref="MonthDayOutOfBoundsStrategy"/> to a string value.
    /// </summary>
    /// <param name="strategy">The strategy to serialize.</param>
    /// <returns>The string value ("skip" or "clamp"), or null if the strategy is null.</returns>
    private static string? SerializeMonthDayBehavior(MonthDayOutOfBoundsStrategy? strategy)
    {
        return strategy switch
        {
            MonthDayOutOfBoundsStrategy.Skip => "skip",
            MonthDayOutOfBoundsStrategy.Clamp => "clamp",
            _ => null
        };
    }
}
