namespace RecurringThings.Models;

using System;

/// <summary>
/// Contains recurrence-specific details when a <see cref="CalendarEntry"/> represents a recurrence pattern.
/// </summary>
/// <remarks>
/// This class is populated on <see cref="CalendarEntry.RecurrenceDetails"/> only when
/// the entry represents the recurrence pattern itself, not a virtualized occurrence.
/// </remarks>
public sealed class RecurrenceDetails
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the recurrence series ends.
    /// </summary>
    /// <remarks>
    /// Matches the UNTIL value in the RRule.
    /// </remarks>
    public DateTime RecurrenceEndTime { get; set; }

    /// <summary>
    /// Gets or sets the RFC 5545 recurrence rule defining the pattern.
    /// </summary>
    /// <remarks>
    /// Example: "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20251231T235959Z"
    /// </remarks>
    public required string RRule { get; set; }
}
