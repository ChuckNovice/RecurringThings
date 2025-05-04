namespace RecurringThings.Domain;

using System;

/// <summary>
///     Represents an exception tied to a specific recurrence, indicating an excluded or invalidated occurrence.
/// </summary>
public class OccurrenceException
{

    /// <summary>
    ///     Gets or sets the unique identifier of the exception.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the original date and time of the occurrence to be excluded.
    /// </summary>
    public DateTime OriginalTime { get; set; }

    /// <summary>
    ///     Navigation property to the recurrence this exception applies to.
    /// </summary>
    public Recurrence Recurrence { get; set; }

}