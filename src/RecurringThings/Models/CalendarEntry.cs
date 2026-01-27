namespace RecurringThings.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Unified abstraction returned from queries, representing recurrences, standalone occurrences,
/// or virtualized occurrences from recurrence patterns.
/// </summary>
/// <remarks>
/// <para>
/// The type of entry can be determined by examining which ID properties are set:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>Recurrence</term>
/// <description><see cref="RecurrenceId"/> is set and <see cref="RecurrenceDetails"/> is populated</description>
/// </item>
/// <item>
/// <term>Standalone Occurrence</term>
/// <description><see cref="OccurrenceId"/> is set and both detail properties are null</description>
/// </item>
/// <item>
/// <term>Virtualized Occurrence</term>
/// <description><see cref="RecurrenceOccurrenceDetails"/> is populated (optionally with <see cref="OverrideId"/>)</description>
/// </item>
/// </list>
/// <para>
/// <see cref="ExceptionId"/> is never set in query results because excepted occurrences are not returned.
/// </para>
/// </remarks>
public sealed class CalendarEntry
{
    /// <summary>
    /// Gets or sets the tenant identifier for multi-tenant isolation.
    /// </summary>
    public required string Organization { get; set; }

    /// <summary>
    /// Gets or sets the hierarchical resource scope.
    /// </summary>
    public required string ResourcePath { get; set; }

    /// <summary>
    /// Gets or sets the user-defined type of this entry.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this entry starts.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this entry ends.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the duration of this entry.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the IANA time zone identifier.
    /// </summary>
    public required string TimeZone { get; set; }

    /// <summary>
    /// Gets or sets the user-defined key-value metadata.
    /// </summary>
    public Dictionary<string, string>? Extensions { get; set; }

    /// <summary>
    /// Gets or sets the recurrence ID if this entry represents a recurrence pattern or a virtualized occurrence.
    /// </summary>
    /// <remarks>
    /// Set when this entry is a recurrence (with <see cref="RecurrenceDetails"/> populated)
    /// or when this is a virtualized occurrence from a recurrence.
    /// </remarks>
    public Guid? RecurrenceId { get; set; }

    /// <summary>
    /// Gets or sets the standalone occurrence ID if this entry represents a standalone occurrence.
    /// </summary>
    /// <remarks>
    /// Set only for standalone occurrences (not from a recurrence pattern).
    /// </remarks>
    public Guid? OccurrenceId { get; set; }

    /// <summary>
    /// Gets or sets the override ID if this virtualized occurrence has been modified.
    /// </summary>
    /// <remarks>
    /// Set when this is a virtualized occurrence that has an override applied.
    /// </remarks>
    public Guid? OverrideId { get; set; }

    /// <summary>
    /// Gets or sets the exception ID.
    /// </summary>
    /// <remarks>
    /// This property is never set in query results because excepted (deleted) occurrences
    /// are not returned by GetAsync queries.
    /// </remarks>
    public Guid? ExceptionId { get; set; }

    /// <summary>
    /// Gets or sets the recurrence-specific details.
    /// </summary>
    /// <remarks>
    /// Populated only when this entry represents a recurrence pattern (not a virtualized occurrence).
    /// Mutually exclusive with <see cref="RecurrenceOccurrenceDetails"/>.
    /// </remarks>
    public RecurrenceDetails? RecurrenceDetails { get; set; }

    /// <summary>
    /// Gets or sets the virtualized occurrence details.
    /// </summary>
    /// <remarks>
    /// Populated when this entry represents an occurrence generated from a recurrence pattern.
    /// Mutually exclusive with <see cref="RecurrenceDetails"/>.
    /// </remarks>
    public RecurrenceOccurrenceDetails? RecurrenceOccurrenceDetails { get; set; }
}
