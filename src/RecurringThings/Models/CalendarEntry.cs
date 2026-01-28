namespace RecurringThings.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Unified abstraction returned from queries, representing recurrences, standalone occurrences,
/// or virtualized occurrences from recurrence patterns.
/// </summary>
/// <remarks>
/// <para>
/// The type of entry can be determined by examining the <see cref="EntryType"/> property:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>Recurrence</term>
/// <description><see cref="EntryType"/> is <see cref="CalendarEntryType.Recurrence"/></description>
/// </item>
/// <item>
/// <term>Standalone Occurrence</term>
/// <description><see cref="EntryType"/> is <see cref="CalendarEntryType.Standalone"/></description>
/// </item>
/// <item>
/// <term>Virtualized Occurrence</term>
/// <description><see cref="EntryType"/> is <see cref="CalendarEntryType.Virtualized"/></description>
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
    /// Gets or sets the type of this calendar entry.
    /// </summary>
    public CalendarEntryType EntryType { get; set; }

    /// <summary>
    /// Gets or sets the local timestamp when this entry starts.
    /// </summary>
    /// <remarks>
    /// The time is in the local timezone specified by <see cref="TimeZone"/>.
    /// </remarks>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the local timestamp when this entry ends.
    /// </summary>
    /// <remarks>
    /// The time is in the local timezone specified by <see cref="TimeZone"/>.
    /// </remarks>
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
    /// Gets or sets the recurrence ID.
    /// </summary>
    /// <remarks>
    /// Set when <see cref="EntryType"/> is <see cref="CalendarEntryType.Recurrence"/>
    /// or <see cref="CalendarEntryType.Virtualized"/>.
    /// </remarks>
    public Guid? RecurrenceId { get; set; }

    /// <summary>
    /// Gets or sets the standalone occurrence ID.
    /// </summary>
    /// <remarks>
    /// Set only when <see cref="EntryType"/> is <see cref="CalendarEntryType.Standalone"/>.
    /// </remarks>
    public Guid? OccurrenceId { get; set; }

    /// <summary>
    /// Gets or sets the override ID if this virtualized occurrence has been modified.
    /// </summary>
    /// <remarks>
    /// Set when this is a virtualized occurrence that has an override applied.
    /// Check <see cref="IsOverridden"/> for a convenient boolean check.
    /// </remarks>
    public Guid? OverrideId { get; set; }

    /// <summary>
    /// Gets or sets the exception ID.
    /// </summary>
    /// <remarks>
    /// This property is never set in query results because excepted (deleted) occurrences
    /// are not returned by queries.
    /// </remarks>
    public Guid? ExceptionId { get; set; }

    /// <summary>
    /// Gets a value indicating whether this entry has an override applied.
    /// </summary>
    /// <remarks>
    /// Returns <c>true</c> only for virtualized occurrences with an override applied.
    /// When <c>true</c>, <see cref="Original"/> contains the original values before the override.
    /// </remarks>
    public bool IsOverridden => OverrideId.HasValue;

    /// <summary>
    /// Gets or sets the recurrence details.
    /// </summary>
    /// <remarks>
    /// Populated when <see cref="EntryType"/> is <see cref="CalendarEntryType.Recurrence"/>
    /// or <see cref="CalendarEntryType.Virtualized"/>. Contains the RRule that defines or
    /// generated this entry.
    /// </remarks>
    public RecurrenceDetails? RecurrenceDetails { get; set; }

    /// <summary>
    /// Gets or sets the original values before an override was applied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated only when <see cref="IsOverridden"/> is <c>true</c>.
    /// </para>
    /// <para>
    /// When an override is applied to a virtualized occurrence, this property contains
    /// the original start time, duration, and extensions before modification.
    /// </para>
    /// </remarks>
    public OriginalDetails? Original { get; set; }
}
