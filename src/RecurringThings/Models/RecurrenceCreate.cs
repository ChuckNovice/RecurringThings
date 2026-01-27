namespace RecurringThings.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Request model for creating a new recurrence pattern.
/// </summary>
/// <remarks>
/// <para>DateTime values can be provided in UTC or Local time (Unspecified is not allowed).
/// Local times are converted to UTC internally using the specified <see cref="TimeZone"/>.</para>
/// <para>The RRule UNTIL value must be in UTC (Z suffix) and must match <see cref="RecurrenceEndTime"/> when converted to UTC.</para>
/// </remarks>
public sealed class RecurrenceCreate
{
    /// <summary>
    /// Gets the tenant identifier for multi-tenant isolation.
    /// </summary>
    /// <remarks>
    /// Must be between 0 and 100 characters. Empty string is allowed for single-tenant scenarios.
    /// </remarks>
    public required string Organization { get; init; }

    /// <summary>
    /// Gets the hierarchical resource scope.
    /// </summary>
    /// <remarks>
    /// Used for organizing resources hierarchically (e.g., "user123/calendar", "store456").
    /// Must be between 0 and 100 characters. Empty string is allowed.
    /// </remarks>
    public required string ResourcePath { get; init; }

    /// <summary>
    /// Gets the user-defined type of this recurrence.
    /// </summary>
    /// <remarks>
    /// Used to differentiate between different kinds of recurring things (e.g., "appointment", "open-hours").
    /// Must be between 1 and 100 characters. Empty string is NOT allowed.
    /// </remarks>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the timestamp representing the time-of-day that occurrences start.
    /// </summary>
    /// <remarks>
    /// Can be provided in UTC or Local time (Unspecified is not allowed).
    /// Local times are converted to UTC internally using <see cref="TimeZone"/>.
    /// During virtualization, the stored UTC time is converted to local time
    /// using <see cref="TimeZone"/> before applying the RRule.
    /// </remarks>
    public required DateTime StartTime { get; init; }

    /// <summary>
    /// Gets the duration of each occurrence.
    /// </summary>
    /// <remarks>
    /// Individual occurrence end times are computed as StartTime + Duration during virtualization.
    /// </remarks>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the timestamp when the recurrence series ends.
    /// </summary>
    /// <remarks>
    /// Can be provided in UTC or Local time (Unspecified is not allowed).
    /// Local times are converted to UTC internally using <see cref="TimeZone"/>.
    /// Must match the UNTIL value in <see cref="RRule"/> when converted to UTC.
    /// Used for efficient query filtering without full virtualization.
    /// </remarks>
    public required DateTime RecurrenceEndTime { get; init; }

    /// <summary>
    /// Gets the RFC 5545 recurrence rule defining the pattern.
    /// </summary>
    /// <remarks>
    /// <para>Must use UNTIL (in UTC with Z suffix); COUNT is not supported.</para>
    /// <para>Maximum length is 2000 characters.</para>
    /// <para>Example: "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20251231T235959Z"</para>
    /// </remarks>
    public required string RRule { get; init; }

    /// <summary>
    /// Gets the IANA time zone identifier.
    /// </summary>
    /// <remarks>
    /// Used for local time conversion during virtualization to correctly handle DST transitions.
    /// Must be a valid IANA timezone (e.g., "America/New_York", not "Eastern Standard Time").
    /// </remarks>
    public required string TimeZone { get; init; }

    /// <summary>
    /// Gets the user-defined key-value metadata.
    /// </summary>
    /// <remarks>
    /// <para>Optional. Can be null.</para>
    /// <para>Key constraints: 1-100 characters, non-null.</para>
    /// <para>Value constraints: 0-1024 characters, non-null.</para>
    /// </remarks>
    public Dictionary<string, string>? Extensions { get; init; }
}
