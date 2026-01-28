namespace RecurringThings.Engine.Virtualization;

using System;
using System.Collections.Generic;
using Ical.Net.DataTypes;
using RecurringThings.Domain;

/// <summary>
/// Generates occurrence dates for a recurrence pattern.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface are responsible for generating virtualized occurrence times
/// from a recurrence pattern within a query range.
/// </para>
/// <para>
/// Different implementations handle different strategies:
/// </para>
/// <list type="bullet">
/// <item><see cref="IcalNetOccurrenceGenerator"/> - Standard Ical.Net evaluation (also handles Skip strategy)</item>
/// <item><see cref="ClampedMonthlyOccurrenceGenerator"/> - Clamps out-of-bounds days to month end</item>
/// </list>
/// </remarks>
internal interface IOccurrenceGenerator
{
    /// <summary>
    /// Generates occurrence dates within the specified range.
    /// </summary>
    /// <param name="recurrence">The recurrence containing the pattern and timezone.</param>
    /// <param name="pattern">The parsed recurrence pattern (parsed once and reused).</param>
    /// <param name="queryStartUtc">The start of the query range in UTC.</param>
    /// <param name="queryEndUtc">The end of the query range in UTC.</param>
    /// <returns>An enumerable of UTC DateTime values representing occurrence times.</returns>
    /// <remarks>
    /// <para>
    /// The returned dates must be in UTC and must fall within the query range
    /// and not exceed the recurrence's end time.
    /// </para>
    /// <para>
    /// The <paramref name="pattern"/> parameter is provided to avoid re-parsing
    /// the RRule string multiple times.
    /// </para>
    /// </remarks>
    IEnumerable<DateTime> GenerateOccurrences(
        Recurrence recurrence,
        RecurrencePattern pattern,
        DateTime queryStartUtc,
        DateTime queryEndUtc);
}
