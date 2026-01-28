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
/// <para>
/// <strong>Mutable properties:</strong>
/// </para>
/// <list type="bullet">
/// <item><see cref="StartTime"/>, <see cref="Duration"/>, <see cref="Extensions"/> - mutable on standalone occurrences and virtualized occurrences</item>
/// <item><see cref="Type"/>, <see cref="ResourcePath"/> - mutable only on standalone occurrences and recurrences (not on virtualized occurrences)</item>
/// </list>
/// </remarks>
public sealed class CalendarEntry
{
    private string _resourcePath = null!;

    /// <summary>
    /// Gets the tenant identifier for multi-tenant isolation.
    /// </summary>
    /// <remarks>
    /// This property is immutable after creation.
    /// </remarks>
    public required string Organization { get; init; }

    /// <summary>
    /// Gets or sets the hierarchical resource scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is mutable on standalone occurrences and recurrences.
    /// Changing ResourcePath on virtualized occurrences is not allowed.
    /// </para>
    /// </remarks>
    public required string ResourcePath
    {
        get => _resourcePath;
        set
        {
            // Track original value for database lookups
            OriginalResourcePath ??= _resourcePath;
            _resourcePath = value;
        }
    }

    /// <summary>
    /// Gets the original resource path value when the entry was loaded.
    /// </summary>
    /// <remarks>
    /// Used internally to locate the record in the database when ResourcePath has been modified.
    /// </remarks>
    internal string? OriginalResourcePath { get; private set; }

    /// <summary>
    /// Gets or sets the user-defined type of this entry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is mutable on standalone occurrences and recurrences.
    /// Changing Type on virtualized occurrences is not allowed.
    /// </para>
    /// </remarks>
    public required string Type { get; set; }

    /// <summary>
    /// Gets the type of this calendar entry.
    /// </summary>
    /// <remarks>
    /// This property is immutable after creation.
    /// </remarks>
    public CalendarEntryType EntryType { get; init; }

    /// <summary>
    /// Gets or sets the local timestamp when this entry starts.
    /// </summary>
    /// <remarks>
    /// The time is in the local timezone specified by <see cref="TimeZone"/>.
    /// </remarks>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets the local timestamp when this entry ends.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The time is in the local timezone specified by <see cref="TimeZone"/>.
    /// </para>
    /// <para>
    /// EndTime is computed as StartTime + Duration and cannot be set directly.
    /// </para>
    /// </remarks>
    public DateTime EndTime { get; internal set; }

    /// <summary>
    /// Gets or sets the duration of this entry.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets the IANA time zone identifier.
    /// </summary>
    /// <remarks>
    /// This property is immutable after creation.
    /// </remarks>
    public required string TimeZone { get; init; }

    /// <summary>
    /// Gets or sets the user-defined key-value metadata.
    /// </summary>
    public Dictionary<string, string>? Extensions { get; set; }

    /// <summary>
    /// Gets the recurrence ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set when <see cref="EntryType"/> is <see cref="CalendarEntryType.Recurrence"/>
    /// or <see cref="CalendarEntryType.Virtualized"/>.
    /// </para>
    /// <para>
    /// This property is immutable after creation.
    /// </para>
    /// </remarks>
    public Guid? RecurrenceId { get; init; }

    /// <summary>
    /// Gets the standalone occurrence ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set only when <see cref="EntryType"/> is <see cref="CalendarEntryType.Standalone"/>.
    /// </para>
    /// <para>
    /// This property is immutable after creation.
    /// </para>
    /// </remarks>
    public Guid? OccurrenceId { get; init; }

    /// <summary>
    /// Gets the override ID if this virtualized occurrence has been modified.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set when this is a virtualized occurrence that has an override applied.
    /// Check <see cref="IsOverridden"/> for a convenient boolean check.
    /// </para>
    /// <para>
    /// This property is immutable after creation.
    /// </para>
    /// </remarks>
    public Guid? OverrideId { get; init; }

    /// <summary>
    /// Gets the exception ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is never set in query results because excepted (deleted) occurrences
    /// are not returned by queries.
    /// </para>
    /// <para>
    /// This property is immutable after creation.
    /// </para>
    /// </remarks>
    public Guid? ExceptionId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entry has an override applied.
    /// </summary>
    /// <remarks>
    /// Returns <c>true</c> only for virtualized occurrences with an override applied.
    /// When <c>true</c>, <see cref="Original"/> contains the original values before the override.
    /// </remarks>
    public bool IsOverridden => OverrideId.HasValue;

    /// <summary>
    /// Gets the recurrence details.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated when <see cref="EntryType"/> is <see cref="CalendarEntryType.Recurrence"/>
    /// or <see cref="CalendarEntryType.Virtualized"/>. Contains the RRule that defines or
    /// generated this entry.
    /// </para>
    /// <para>
    /// This property is immutable after creation.
    /// </para>
    /// </remarks>
    public RecurrenceDetails? RecurrenceDetails { get; init; }

    /// <summary>
    /// Gets the original values before an override was applied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated only when <see cref="IsOverridden"/> is <c>true</c>.
    /// </para>
    /// <para>
    /// When an override is applied to a virtualized occurrence, this property contains
    /// the original start time, duration, and extensions before modification.
    /// </para>
    /// <para>
    /// This property is immutable after creation.
    /// </para>
    /// </remarks>
    public OriginalDetails? Original { get; init; }
}
