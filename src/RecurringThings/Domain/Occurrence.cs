namespace RecurringThings.Domain;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a standalone (non-recurring) event occurrence.
/// </summary>
/// <remarks>
/// <para>
/// Unlike virtualized occurrences generated from a <see cref="Recurrence"/>, standalone occurrences
/// are stored directly in the database.
/// </para>
/// <para>
/// After creation, <see cref="StartTime"/>, <see cref="Duration"/>, and <see cref="Extensions"/> can be modified.
/// When StartTime or Duration changes, <see cref="EndTime"/> is automatically recomputed.
/// </para>
/// </remarks>
public sealed class Occurrence
{
    private DateTime _startTime;
    private TimeSpan _duration;

    /// <summary>
    /// Gets the unique identifier for this occurrence.
    /// </summary>
    public Guid Id { get; init; }

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
    /// Gets or sets the UTC timestamp when this occurrence starts.
    /// </summary>
    /// <remarks>
    /// When modified, <see cref="EndTime"/> is automatically recomputed.
    /// </remarks>
    public DateTime StartTime
    {
        get => _startTime;
        set
        {
            _startTime = value;
            EndTime = _startTime + _duration;
        }
    }

    /// <summary>
    /// Gets the UTC timestamp when this occurrence ends.
    /// </summary>
    /// <remarks>
    /// This value is computed as <see cref="StartTime"/> + <see cref="Duration"/>.
    /// It is automatically recomputed when StartTime or Duration changes.
    /// </remarks>
    public DateTime EndTime { get; private set; }

    /// <summary>
    /// Gets or sets the duration of this occurrence.
    /// </summary>
    /// <remarks>
    /// When modified, <see cref="EndTime"/> is automatically recomputed.
    /// </remarks>
    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            _duration = value;
            EndTime = _startTime + _duration;
        }
    }

    /// <summary>
    /// Gets the IANA time zone identifier.
    /// </summary>
    /// <remarks>
    /// Must be a valid IANA timezone (e.g., "America/New_York", not "Eastern Standard Time").
    /// </remarks>
    public required string TimeZone { get; init; }

    /// <summary>
    /// Gets or sets the user-defined key-value metadata.
    /// </summary>
    /// <remarks>
    /// <para>Key constraints: 1-100 characters, non-null.</para>
    /// <para>Value constraints: 0-1024 characters, non-null.</para>
    /// </remarks>
    public Dictionary<string, string>? Extensions { get; set; }

    /// <summary>
    /// Initializes the occurrence with computed EndTime.
    /// </summary>
    /// <param name="startTime">The UTC start time.</param>
    /// <param name="duration">The duration of the occurrence.</param>
    public void Initialize(DateTime startTime, TimeSpan duration)
    {
        _startTime = startTime;
        _duration = duration;
        EndTime = _startTime + _duration;
    }
}
