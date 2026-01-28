namespace RecurringThings.Models;

/// <summary>
/// Specifies the type of a calendar entry.
/// </summary>
public enum CalendarEntryType
{
    /// <summary>
    /// A standalone occurrence that is not part of a recurrence pattern.
    /// </summary>
    Standalone,

    /// <summary>
    /// A virtualized occurrence generated from a recurrence pattern.
    /// </summary>
    Virtualized,

    /// <summary>
    /// A recurrence pattern that generates virtualized occurrences.
    /// </summary>
    Recurrence
}
