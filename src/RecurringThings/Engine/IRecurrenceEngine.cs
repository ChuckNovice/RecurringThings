namespace RecurringThings.Engine;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecurringThings.Domain;
using RecurringThings.Models;
using Transactional.Abstractions;

/// <summary>
/// Defines the contract for the recurrence virtualization engine.
/// </summary>
/// <remarks>
/// The recurrence engine is responsible for:
/// <list type="bullet">
/// <item>Querying recurrences and standalone occurrences from repositories</item>
/// <item>Virtualizing occurrences from recurrence patterns using Ical.Net</item>
/// <item>Applying occurrence exceptions (cancellations)</item>
/// <item>Applying occurrence overrides (modifications)</item>
/// <item>Returning unified <see cref="CalendarEntry"/> results</item>
/// </list>
/// </remarks>
public interface IRecurrenceEngine
{
    /// <summary>
    /// Gets all occurrences (standalone and virtualized) that overlap with the specified date range.
    /// </summary>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="start">The start of the date range. Can be UTC or Local time (Unspecified is not allowed).</param>
    /// <param name="end">The end of the date range. Can be UTC or Local time (Unspecified is not allowed).</param>
    /// <param name="types">Optional type filter. Null returns all types.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An async enumerable of <see cref="CalendarEntry"/> objects representing:
    /// <list type="bullet">
    /// <item>Standalone occurrences in the range</item>
    /// <item>Virtualized occurrences from recurrences in the range</item>
    /// </list>
    /// Times in returned entries are converted to local time based on each entry's TimeZone.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Recurrence patterns themselves are not returned, only their virtualized occurrences.
    /// Use <see cref="GetRecurrencesAsync"/> to retrieve recurrence patterns.
    /// </para>
    /// <para>
    /// Excepted (cancelled) occurrences are excluded from results.
    /// </para>
    /// <para>
    /// Overridden occurrences are returned with the override values and original values populated.
    /// </para>
    /// <para>
    /// Local times are converted to UTC internally for querying. The conversion uses the system's
    /// local timezone, so for consistent behavior across environments, prefer passing UTC times.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="start"/> or <paramref name="end"/> has DateTimeKind.Unspecified.
    /// </exception>
    IAsyncEnumerable<CalendarEntry> GetOccurrencesAsync(
        string organization,
        string resourcePath,
        DateTime start,
        DateTime end,
        string[]? types,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all recurrence patterns that overlap with the specified date range.
    /// </summary>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="start">The start of the date range. Can be UTC or Local time (Unspecified is not allowed).</param>
    /// <param name="end">The end of the date range. Can be UTC or Local time (Unspecified is not allowed).</param>
    /// <param name="types">Optional type filter. Null returns all types.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An async enumerable of <see cref="CalendarEntry"/> objects representing recurrence patterns.
    /// Each entry has <see cref="CalendarEntry.EntryType"/> set to <see cref="CalendarEntryType.Recurrence"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns only the recurrence patterns themselves, not their virtualized occurrences.
    /// Use <see cref="GetOccurrencesAsync"/> to retrieve virtualized occurrences.
    /// </para>
    /// <para>
    /// A recurrence overlaps with the date range if its recurrence time window
    /// (StartTime to RecurrenceEndTime extracted from UNTIL) intersects with the query range.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="start"/> or <paramref name="end"/> has DateTimeKind.Unspecified.
    /// </exception>
    IAsyncEnumerable<CalendarEntry> GetRecurrencesAsync(
        string organization,
        string resourcePath,
        DateTime start,
        DateTime end,
        string[]? types,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new recurrence pattern.
    /// </summary>
    /// <param name="organization">The tenant identifier for multi-tenant isolation (0-100 chars).</param>
    /// <param name="resourcePath">The hierarchical resource scope (0-100 chars).</param>
    /// <param name="type">The user-defined type of this recurrence (1-100 chars).</param>
    /// <param name="startTime">The timestamp when occurrences start. Can be UTC or Local (Unspecified not allowed).</param>
    /// <param name="duration">The duration of each occurrence.</param>
    /// <param name="rrule">The RFC 5545 recurrence rule. Must use UNTIL in UTC (Z suffix); COUNT is not supported.</param>
    /// <param name="timeZone">The IANA time zone identifier (e.g., "America/New_York").</param>
    /// <param name="extensions">Optional user-defined key-value metadata.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created <see cref="Recurrence"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when validation fails (invalid RRule, missing UNTIL, COUNT used, field length violations, etc.).
    /// </exception>
    /// <example>
    /// <code>
    /// var pattern = new RecurrencePattern
    /// {
    ///     Frequency = FrequencyType.Daily,
    ///     Until = new CalDateTime(new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Local).ToUniversalTime())
    /// };
    /// pattern.ByDay.Add(new WeekDay(DayOfWeek.Monday));
    ///
    /// var recurrence = await engine.CreateRecurrenceAsync(
    ///     organization: "tenant1",
    ///     resourcePath: "user123/calendar",
    ///     type: "appointment",
    ///     startTime: DateTime.Now,
    ///     duration: TimeSpan.FromHours(1),
    ///     rrule: pattern.ToString(),
    ///     timeZone: "America/New_York");
    /// </code>
    /// </example>
    Task<Recurrence> CreateRecurrenceAsync(
        string organization,
        string resourcePath,
        string type,
        DateTime startTime,
        TimeSpan duration,
        string rrule,
        string timeZone,
        Dictionary<string, string>? extensions = null,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new standalone occurrence.
    /// </summary>
    /// <param name="organization">The tenant identifier for multi-tenant isolation (0-100 chars).</param>
    /// <param name="resourcePath">The hierarchical resource scope (0-100 chars).</param>
    /// <param name="type">The user-defined type of this occurrence (1-100 chars).</param>
    /// <param name="startTime">The timestamp when this occurrence starts. Can be UTC or Local (Unspecified not allowed).</param>
    /// <param name="duration">The duration of this occurrence.</param>
    /// <param name="timeZone">The IANA time zone identifier (e.g., "America/New_York").</param>
    /// <param name="extensions">Optional user-defined key-value metadata.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created <see cref="Occurrence"/>.</returns>
    /// <remarks>
    /// EndTime is automatically computed as StartTime + Duration.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when validation fails (field length violations, invalid time zone, etc.).
    /// </exception>
    /// <example>
    /// <code>
    /// var occurrence = await engine.CreateOccurrenceAsync(
    ///     organization: "tenant1",
    ///     resourcePath: "user123/calendar",
    ///     type: "meeting",
    ///     startTime: DateTime.Now,
    ///     duration: TimeSpan.FromMinutes(30),
    ///     timeZone: "America/New_York");
    /// </code>
    /// </example>
    Task<Occurrence> CreateOccurrenceAsync(
        string organization,
        string resourcePath,
        string type,
        DateTime startTime,
        TimeSpan duration,
        string timeZone,
        Dictionary<string, string>? extensions = null,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an occurrence (standalone or virtualized).
    /// </summary>
    /// <param name="entry">The calendar entry with updated values.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="CalendarEntry"/>.</returns>
    /// <remarks>
    /// <para>
    /// Recurrence patterns cannot be updated. To modify a recurrence, delete and recreate it.
    /// </para>
    /// <para>
    /// Update behavior varies by entry type:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Standalone Occurrence</term>
    /// <description><c>StartTime</c>, <c>Duration</c>, and <c>Extensions</c> can be modified. EndTime is recomputed.</description>
    /// </item>
    /// <item>
    /// <term>Virtualized Occurrence (no override)</term>
    /// <description>Creates a new override with denormalized original values from the parent recurrence.</description>
    /// </item>
    /// <item>
    /// <term>Virtualized Occurrence (with override)</term>
    /// <description>Updates the existing override. EndTime is recomputed.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to update a recurrence pattern, or when attempting to modify
    /// immutable fields (Organization, ResourcePath, Type, TimeZone).
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the underlying entity (occurrence or override) is not found.
    /// </exception>
    /// <example>
    /// <code>
    /// // Update a standalone occurrence's start time
    /// entry.StartTime = entry.StartTime.AddHours(1);
    /// var updated = await engine.UpdateOccurrenceAsync(entry);
    ///
    /// // Modify a virtualized occurrence (creates override)
    /// entry.Duration = TimeSpan.FromMinutes(45);
    /// var updated = await engine.UpdateOccurrenceAsync(entry);
    /// </code>
    /// </example>
    Task<CalendarEntry> UpdateOccurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an occurrence (standalone or virtualized).
    /// </summary>
    /// <param name="entry">The calendar entry to delete.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <remarks>
    /// <para>
    /// This method deletes individual occurrences. To delete an entire recurrence pattern
    /// and all its data, use <see cref="DeleteRecurrenceAsync"/>.
    /// </para>
    /// <para>
    /// Delete behavior varies by entry type:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Standalone Occurrence</term>
    /// <description>Deletes the occurrence directly.</description>
    /// </item>
    /// <item>
    /// <term>Virtualized Occurrence (no override)</term>
    /// <description>Creates an occurrence exception to cancel the virtualized occurrence.</description>
    /// </item>
    /// <item>
    /// <term>Virtualized Occurrence (with override)</term>
    /// <description>Deletes the override and creates an exception at the original time.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entry is a recurrence pattern. Use <see cref="DeleteRecurrenceAsync"/> instead.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the underlying entity (occurrence or override) is not found.
    /// </exception>
    /// <example>
    /// <code>
    /// // Cancel a single virtualized occurrence (creates exception)
    /// await engine.DeleteOccurrenceAsync(virtualizedOccurrenceEntry);
    ///
    /// // Delete a standalone occurrence
    /// await engine.DeleteOccurrenceAsync(standaloneEntry);
    /// </code>
    /// </example>
    Task DeleteOccurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a recurrence pattern and all associated exceptions and overrides.
    /// </summary>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="recurrenceId">The ID of the recurrence to delete.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <remarks>
    /// <para>
    /// This operation performs a cascade delete:
    /// </para>
    /// <list type="number">
    /// <item>Deletes all occurrence exceptions for the recurrence</item>
    /// <item>Deletes all occurrence overrides for the recurrence</item>
    /// <item>Deletes the recurrence pattern itself</item>
    /// </list>
    /// <para>
    /// The organization and resourcePath parameters ensure multi-tenant isolation.
    /// </para>
    /// </remarks>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the recurrence is not found.
    /// </exception>
    /// <example>
    /// <code>
    /// // Delete a recurrence and all its data
    /// await engine.DeleteRecurrenceAsync("tenant1", "user123/calendar", recurrenceId);
    /// </code>
    /// </example>
    Task DeleteRecurrenceAsync(
        string organization,
        string resourcePath,
        Guid recurrenceId,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an overridden virtualized occurrence to its original state.
    /// </summary>
    /// <param name="entry">The calendar entry to restore.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous restore operation.</returns>
    /// <remarks>
    /// <para>
    /// This operation is only valid for virtualized occurrences that have an override applied
    /// (<see cref="CalendarEntry.IsOverridden"/> is true).
    /// The override is deleted, and the occurrence will revert to its virtualized state
    /// (computed from the parent recurrence) on the next query.
    /// </para>
    /// <para>
    /// <b>Important:</b> Excepted (deleted) occurrences cannot be restored because they are not
    /// returned by <see cref="GetOccurrencesAsync"/>. To restore an excepted occurrence, you must delete
    /// the exception directly from the exception repository.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to restore:
    /// <list type="bullet">
    /// <item>A recurrence pattern</item>
    /// <item>A standalone occurrence</item>
    /// <item>A virtualized occurrence without an override (<see cref="CalendarEntry.IsOverridden"/> is false)</item>
    /// </list>
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the override is not found.
    /// </exception>
    /// <example>
    /// <code>
    /// // Get an overridden occurrence
    /// var entries = await engine.GetOccurrencesAsync(org, path, start, end, null).ToListAsync();
    /// var overriddenEntry = entries.First(e => e.IsOverridden);
    ///
    /// // Restore it to original virtualized state
    /// await engine.RestoreAsync(overriddenEntry);
    ///
    /// // Next query will return the occurrence with original recurrence values
    /// </code>
    /// </example>
    Task RestoreAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);
}
