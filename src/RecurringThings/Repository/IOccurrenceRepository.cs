namespace RecurringThings.Repository;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecurringThings.Domain;

/// <summary>
/// Repository interface for managing standalone <see cref="Occurrence"/> entities.
/// </summary>
public interface IOccurrenceRepository
{
    /// <summary>
    /// Creates a new standalone occurrence.
    /// </summary>
    /// <param name="occurrence">The occurrence to create.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created occurrence.</returns>
    Task<Occurrence> CreateAsync(
        Occurrence occurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an occurrence by its identifier.
    /// </summary>
    /// <param name="id">The occurrence identifier.</param>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The occurrence if found; otherwise, null.</returns>
    Task<Occurrence?> GetByIdAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing occurrence.
    /// </summary>
    /// <param name="occurrence">The occurrence with updated values.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated occurrence.</returns>
    Task<Occurrence> UpdateAsync(
        Occurrence occurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an occurrence.
    /// </summary>
    /// <param name="id">The occurrence identifier.</param>
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
    /// Gets all occurrences that overlap with the specified date range.
    /// </summary>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="startUtc">The start of the date range (UTC).</param>
    /// <param name="endUtc">The end of the date range (UTC).</param>
    /// <param name="types">Optional type filter. Null returns all types.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of matching occurrences.</returns>
    /// <remarks>
    /// Filters by: StartTime &lt;= endUtc AND EndTime &gt;= startUtc
    /// </remarks>
    IAsyncEnumerable<Occurrence> GetInRangeAsync(
        string organization,
        string resourcePath,
        DateTime startUtc,
        DateTime endUtc,
        string[]? types,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);
}
