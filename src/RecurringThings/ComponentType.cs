namespace RecurringThings;

/// <summary>
/// Specifies the type of iCalendar component.
/// </summary>
public enum ComponentType
{
    /// <summary>
    /// A calendar event (VEVENT).
    /// </summary>
    Event = 0,

    /// <summary>
    /// A to-do item (VTODO).
    /// </summary>
    Todo = 1,

    /// <summary>
    /// A journal entry (VJOURNAL).
    /// </summary>
    Journal = 2
}
