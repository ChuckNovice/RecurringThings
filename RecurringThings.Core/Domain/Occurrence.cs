namespace RecurringThings.Core.Domain;

using System;

/// <summary>
///     Represents a single scheduled occurrence.
/// </summary>
public class Occurrence :
    ICalendarEntry
{

    /// <summary>
    ///     Gets or sets the unique identifier of the occurrence.
    /// </summary>
    public Guid Id { get; set; }

    /// <inheritdoc />
    public DateTime StartTime { get; set; }

    /// <inheritdoc />
    public TimeSpan Duration { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <summary>
    ///     Converts the current occurrence to <see cref="VirtualizedOccurrence"/>.
    /// </summary>
    /// <returns></returns>
    public VirtualizedOccurrence AsVirtualized() =>
        new()
        {
            Id = Id,
            StartTime = StartTime.ToUniversalTime(),
            Duration = Duration,
            Description = Description
        };

}

