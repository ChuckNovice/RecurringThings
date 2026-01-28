namespace RecurringThings.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Contains the original values of a virtualized occurrence before an override was applied.
/// </summary>
/// <remarks>
/// This class is populated on <see cref="CalendarEntry.Original"/> when
/// the virtualized occurrence has been modified by an override.
/// </remarks>
public sealed class OriginalDetails
{
    /// <summary>
    /// Gets or sets the original UTC start time before the override was applied.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the original duration before the override was applied.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the original extensions before the override was applied.
    /// </summary>
    public Dictionary<string, string>? Extensions { get; set; }
}
