namespace RecurringThings.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Request model for creating a new standalone occurrence.
/// </summary>
/// <remarks>
/// <para>All DateTime values must be provided in UTC.</para>
/// <para>EndTime will be computed automatically as StartTimeUtc + Duration.</para>
/// </remarks>
public sealed class OccurrenceCreate
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
    /// Gets the user-defined type of this occurrence.
    /// </summary>
    /// <remarks>
    /// Used to differentiate between different kinds of occurrences (e.g., "appointment", "meeting").
    /// Must be between 1 and 100 characters. Empty string is NOT allowed.
    /// </remarks>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this occurrence starts.
    /// </summary>
    /// <remarks>
    /// Must be provided in UTC.
    /// </remarks>
    public required DateTime StartTimeUtc { get; init; }

    /// <summary>
    /// Gets the duration of this occurrence.
    /// </summary>
    /// <remarks>
    /// EndTime will be computed as StartTimeUtc + Duration.
    /// </remarks>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the IANA time zone identifier.
    /// </summary>
    /// <remarks>
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
