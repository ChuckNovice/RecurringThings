namespace RecurringThings.Repository;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecurringThings.Domain;
using Transactional.Abstractions;

/// <summary>
/// Repository interface for managing <see cref="OccurrenceOverride"/> entities.
/// </summary>
/// <remarks>
/// Occurrence overrides are used to modify specific virtualized occurrences
/// from a recurrence pattern (e.g., change time, duration, or metadata).
/// </remarks>
public interface IOccurrenceOverrideRepository
{
    /// <summary>
    /// Creates a new occurrence override.
    /// </summary>
    /// <param name="override">The override to create.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created override.</returns>
    Task<OccurrenceOverride> CreateAsync(
        OccurrenceOverride @override,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an override by its identifier.
    /// </summary>
    /// <param name="id">The override identifier.</param>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The override if found; otherwise, null.</returns>
    Task<OccurrenceOverride?> GetByIdAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all overrides for a specific recurrence.
    /// </summary>
    /// <param name="recurrenceId">The parent recurrence identifier.</param>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of overrides for the recurrence.</returns>
    IAsyncEnumerable<OccurrenceOverride> GetByRecurrenceIdAsync(
        Guid recurrenceId,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all overrides for recurrences that have occurrences in the specified date range.
    /// </summary>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="recurrenceIds">The recurrence identifiers to get overrides for.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of matching overrides.</returns>
    IAsyncEnumerable<OccurrenceOverride> GetByRecurrenceIdsAsync(
        string organization,
        string resourcePath,
        IEnumerable<Guid> recurrenceIds,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all overrides that overlap with the specified date range.
    /// </summary>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="recurrenceIds">The recurrence identifiers to get overrides for.</param>
    /// <param name="startUtc">The start of the date range (UTC).</param>
    /// <param name="endUtc">The end of the date range (UTC).</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of matching overrides.</returns>
    /// <remarks>
    /// Filters by: StartTime &lt;= endUtc AND EndTime &gt;= startUtc
    /// </remarks>
    IAsyncEnumerable<OccurrenceOverride> GetInRangeAsync(
        string organization,
        string resourcePath,
        IEnumerable<Guid> recurrenceIds,
        DateTime startUtc,
        DateTime endUtc,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing override.
    /// </summary>
    /// <param name="override">The override with updated values.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated override.</returns>
    Task<OccurrenceOverride> UpdateAsync(
        OccurrenceOverride @override,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an override.
    /// </summary>
    /// <param name="id">The override identifier.</param>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    Task DeleteAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all overrides for a specific recurrence.
    /// </summary>
    /// <param name="recurrenceId">The parent recurrence identifier.</param>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    Task DeleteByRecurrenceIdAsync(
        Guid recurrenceId,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);
}
