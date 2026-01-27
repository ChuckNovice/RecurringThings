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
    /// Gets all calendar entries that overlap with the specified date range.
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
    IAsyncEnumerable<CalendarEntry> GetAsync(
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
    /// <param name="request">The recurrence creation request.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created <see cref="Recurrence"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when validation fails (invalid RRule, missing UNTIL, COUNT used, field length violations, etc.).
    /// </exception>
    /// <example>
    /// <code>
    /// var recurrence = await engine.CreateRecurrenceAsync(new RecurrenceCreate
    /// {
    ///     Organization = "tenant1",
    ///     ResourcePath = "user123/calendar",
    ///     Type = "appointment",
    ///     StartTime = DateTime.UtcNow, // Or DateTime.Now for local time
    ///     Duration = TimeSpan.FromHours(1),
    ///     RecurrenceEndTime = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
    ///     RRule = "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20251231T235959Z",
    ///     TimeZone = "America/New_York"
    /// });
    /// </code>
    /// </example>
    Task<Recurrence> CreateRecurrenceAsync(
        RecurrenceCreate request,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new standalone occurrence.
    /// </summary>
    /// <param name="request">The occurrence creation request.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created <see cref="Occurrence"/>.</returns>
    /// <remarks>
    /// EndTime is automatically computed as StartTime + Duration.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when validation fails (field length violations, invalid time zone, etc.).
    /// </exception>
    /// <example>
    /// <code>
    /// var occurrence = await engine.CreateOccurrenceAsync(new OccurrenceCreate
    /// {
    ///     Organization = "tenant1",
    ///     ResourcePath = "user123/calendar",
    ///     Type = "meeting",
    ///     StartTime = DateTime.UtcNow, // Or DateTime.Now for local time
    ///     Duration = TimeSpan.FromMinutes(30),
    ///     TimeZone = "America/New_York"
    /// });
    /// </code>
    /// </example>
    Task<Occurrence> CreateOccurrenceAsync(
        OccurrenceCreate request,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a calendar entry with immutability enforcement.
    /// </summary>
    /// <param name="entry">The calendar entry with updated values.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="CalendarEntry"/>.</returns>
    /// <remarks>
    /// <para>
    /// Update behavior varies by entry type:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Recurrence</term>
    /// <description>Only <c>Duration</c> and <c>Extensions</c> can be modified.</description>
    /// </item>
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
    /// Thrown when attempting to modify immutable fields (Organization, ResourcePath, Type, TimeZone,
    /// or recurrence-specific fields like RRule, StartTime, RecurrenceEndTime).
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the underlying entity (recurrence, occurrence, or override) is not found.
    /// </exception>
    /// <example>
    /// <code>
    /// // Update a recurrence's duration
    /// entry.Duration = TimeSpan.FromHours(2);
    /// var updated = await engine.UpdateAsync(entry);
    ///
    /// // Update a standalone occurrence's start time
    /// entry.StartTime = entry.StartTime.AddHours(1);
    /// var updated = await engine.UpdateAsync(entry);
    ///
    /// // Modify a virtualized occurrence (creates override)
    /// entry.Duration = TimeSpan.FromMinutes(45);
    /// var updated = await engine.UpdateAsync(entry);
    /// </code>
    /// </example>
    Task<CalendarEntry> UpdateAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a calendar entry with appropriate cascade behavior.
    /// </summary>
    /// <param name="entry">The calendar entry to delete.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <remarks>
    /// <para>
    /// Delete behavior varies by entry type:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term>Recurrence</term>
    /// <description>Deletes the entire series including all exceptions and overrides (cascade delete).</description>
    /// </item>
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
    /// Thrown when the entry type cannot be determined.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the underlying entity (recurrence, occurrence, or override) is not found.
    /// </exception>
    /// <example>
    /// <code>
    /// // Delete an entire recurrence series
    /// await engine.DeleteAsync(recurrenceEntry);
    ///
    /// // Cancel a single virtualized occurrence (creates exception)
    /// await engine.DeleteAsync(virtualizedOccurrenceEntry);
    ///
    /// // Delete a standalone occurrence
    /// await engine.DeleteAsync(standaloneEntry);
    /// </code>
    /// </example>
    Task DeleteAsync(
        CalendarEntry entry,
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
    /// This operation is only valid for virtualized occurrences that have an override applied.
    /// The override is deleted, and the occurrence will revert to its virtualized state
    /// (computed from the parent recurrence) on the next query.
    /// </para>
    /// <para>
    /// <b>Important:</b> Excepted (deleted) occurrences cannot be restored because they are not
    /// returned by <see cref="GetAsync"/>. To restore an excepted occurrence, you must delete
    /// the exception directly from the exception repository.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to restore:
    /// <list type="bullet">
    /// <item>A recurrence pattern (RecurrenceId set without RecurrenceOccurrenceDetails)</item>
    /// <item>A standalone occurrence (OccurrenceId set)</item>
    /// <item>A virtualized occurrence without an override (OverrideId is null)</item>
    /// </list>
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the override is not found.
    /// </exception>
    /// <example>
    /// <code>
    /// // Get an overridden occurrence
    /// var entries = await engine.GetAsync(org, path, start, end, null).ToListAsync();
    /// var overriddenEntry = entries.First(e => e.OverrideId.HasValue);
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
