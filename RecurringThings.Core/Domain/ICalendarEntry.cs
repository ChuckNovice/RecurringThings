namespace RecurringThings.Core.Domain;

/// <summary>
///     Represents the attributes shared by all calendar entries.
/// </summary>
public interface ICalendarEntry
{

    /// <summary>
    ///     Gets or sets the start time.
    /// </summary>
    DateTime StartTime { get; set; }

    /// <summary>
    ///     Gets or sets the duration.
    /// </summary>
    TimeSpan Duration { get; set; }

    /// <summary>
    ///     A detailed description.
    /// </summary>
    string Description { get; set; }

}
