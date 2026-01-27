namespace RecurringThings.Models;

using System;

/// <summary>
/// Contains details for a <see cref="CalendarEntry"/> that represents a virtualized occurrence
/// generated from a recurrence pattern.
/// </summary>
/// <remarks>
/// This class is populated on <see cref="CalendarEntry.RecurrenceOccurrenceDetails"/> when
/// the entry is an occurrence generated from a recurrence's RRule pattern.
/// </remarks>
public sealed class RecurrenceOccurrenceDetails
{
    /// <summary>
    /// Gets or sets the identifier of the parent recurrence that generated this occurrence.
    /// </summary>
    public Guid RecurrenceId { get; set; }

    /// <summary>
    /// Gets or sets the original values before an override was applied.
    /// </summary>
    /// <remarks>
    /// <para>Non-null when this occurrence has an override applied.</para>
    /// <para>Null when this is a "clean" virtualized occurrence without modifications.</para>
    /// </remarks>
    public OccurrenceOriginal? Original { get; set; }
}
