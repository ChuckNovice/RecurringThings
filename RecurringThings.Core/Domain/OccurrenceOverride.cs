namespace RecurringThings.Core.Domain;

using System;

/// <summary>
///     Represents a modified instance of a recurring occurrence with a new time and optional note.
/// </summary>
public class OccurrenceOverride :
    ICalendarEntry
{

    /// <summary>
    ///     Gets or sets the unique identifier of the override.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the original occurrence time being overridden.
    /// </summary>
    public DateTime OriginalTime { get; set; }

    /// <inheritdoc />
    public DateTime StartTime { get; set; }

    /// <inheritdoc />
    public TimeSpan Duration { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <summary>
    ///     Gets or sets the recurrence this override applies to.
    /// </summary>
    public Recurrence Recurrence { get; set; }

    /// <summary>
    ///     Converts the current occurrence to <see cref="VirtualizedOccurrence"/>.
    /// </summary>
    /// <returns></returns>
    public VirtualizedOccurrence AsVirtualized() =>
        new()
        {
            Recurrence = Recurrence,
            StartTime = StartTime.ToUniversalTime(),
            Duration = Duration,
            Description = Description
        };

}