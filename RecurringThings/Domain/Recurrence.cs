namespace RecurringThings.Domain;

using System;

/// <summary>
///     Represents a recurring schedule pattern with RRULE and time range.
/// </summary>
public class Recurrence :
    ICalendarEntry
{

    /// <summary>
    ///     Gets or sets the unique identifier of the recurrence pattern.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the RRULE string defining the recurrence pattern.
    /// </summary>
    public string RRule { get; set; }

    /// <summary>
    ///     Gets or sets the IANA time zone ID the recurrence applies to.
    /// </summary>
    public string TimeZone { get; set; }

    /// <inheritdoc />
    public DateTime StartTime { get; set; }

    /// <inheritdoc />
    public TimeSpan Duration { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <summary>
    ///     Gets or sets the end time of the recurrence, which states when the series should end.
    /// </summary>
    public DateTime RecurrenceEndTime { get; set; }

    /// <summary>
    ///     Gets or sets the exceptions declared for this recurrence.
    /// </summary>
    public ICollection<OccurrenceException> Exceptions { get; set; } = [];

    /// <summary>
    ///     Gets or sets the overrides applied to this recurrence.
    /// </summary>
    public ICollection<OccurrenceOverride> Overrides { get; set; } = [];

    /// <summary>
    ///     Converts the current recurrence to <see cref="VirtualizedOccurrence"/>.
    /// </summary>
    /// <returns></returns>
    public VirtualizedOccurrence AsVirtualized() =>
        new()
        {
            Recurrence = this,
            StartTime = StartTime.ToUniversalTime(),
            Duration = Duration,
            Description = Description,
        };

}