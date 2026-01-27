namespace RecurringThings.Repository;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecurringThings.Domain;
using Transactional.Abstractions;

/// <summary>
/// Repository interface for managing <see cref="Recurrence"/> entities.
/// </summary>
public interface IRecurrenceRepository
{
    /// <summary>
    /// Creates a new recurrence.
    /// </summary>
    /// <param name="recurrence">The recurrence to create.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created recurrence.</returns>
    Task<Recurrence> CreateAsync(
        Recurrence recurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a recurrence by its identifier.
    /// </summary>
    /// <param name="id">The recurrence identifier.</param>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The recurrence if found; otherwise, null.</returns>
    Task<Recurrence?> GetByIdAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing recurrence.
    /// </summary>
    /// <param name="recurrence">The recurrence with updated values.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated recurrence.</returns>
    Task<Recurrence> UpdateAsync(
        Recurrence recurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a recurrence and all associated exceptions and overrides.
    /// </summary>
    /// <param name="id">The recurrence identifier.</param>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <remarks>
    /// Cascade delete behavior is database-specific:
    /// MongoDB uses application-level cascade within a transaction.
    /// PostgreSQL uses ON DELETE CASCADE foreign key constraints.
    /// </remarks>
    Task DeleteAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all recurrences that potentially overlap with the specified date range.
    /// </summary>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="startUtc">The start of the date range (UTC).</param>
    /// <param name="endUtc">The end of the date range (UTC).</param>
    /// <param name="types">Optional type filter. Null returns all types.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of matching recurrences.</returns>
    /// <remarks>
    /// Filters by: StartTime &lt;= endUtc AND RecurrenceEndTime &gt;= startUtc
    /// </remarks>
    IAsyncEnumerable<Recurrence> GetInRangeAsync(
        string organization,
        string resourcePath,
        DateTime startUtc,
        DateTime endUtc,
        string[]? types,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);
}
