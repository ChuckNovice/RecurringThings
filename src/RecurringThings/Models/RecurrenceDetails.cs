namespace RecurringThings.Models;

using RecurringThings.Options;

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

    /// <summary>
    /// Gets or sets the strategy used for handling out-of-bounds days in monthly recurrences.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Null when the recurrence doesn't have out-of-bounds day handling configured,
    /// which includes:
    /// </para>
    /// <list type="bullet">
    /// <item>Non-monthly patterns (daily, weekly, yearly)</item>
    /// <item>Monthly patterns with BYMONTHDAY values &lt;= 28</item>
    /// <item>Monthly patterns where all months in the range can accommodate the specified day</item>
    /// </list>
    /// <para>
    /// When set, this indicates how virtualization handles months where the specified day
    /// doesn't exist (e.g., 31st in April).
    /// </para>
    /// </remarks>
    public MonthDayOutOfBoundsStrategy? MonthDayBehavior { get; init; }
}
