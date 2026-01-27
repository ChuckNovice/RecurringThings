namespace RecurringThings.Models;

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
    /// Gets or sets the RFC 5545 recurrence rule defining the pattern.
    /// </summary>
    /// <remarks>
    /// <para>Example: "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20251231T235959Z"</para>
    /// <para>The recurrence end time can be extracted by parsing the UNTIL clause from this RRule.</para>
    /// </remarks>
    public required string RRule { get; set; }
}
