namespace RecurringThings.Core.Domain;

using System;

/// <summary>
///     Represents a virtualized occurrence, whether it was materialized from a recurrence or a standalone occurrence.
/// </summary>
public class VirtualizedOccurrence :
    ICalendarEntry
{

    /// <summary>
    ///     Gets or sets the unique identifier of the occurrence, or null if it was materialized from a recurrence.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    ///     Navigation property to the recurrence that generated this occurrence, or null when it's a standalone occurrence.
    /// </summary>
    public Recurrence Recurrence { get; set; }

    /// <inheritdoc />
    public DateTime StartTime { get; set; }

    /// <inheritdoc />
    public TimeSpan Duration { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

}

