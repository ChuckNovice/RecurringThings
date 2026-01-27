namespace RecurringThings.Repository;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecurringThings.Domain;

/// <summary>
/// Repository interface for managing <see cref="OccurrenceException"/> entities.
/// </summary>
/// <remarks>
/// Occurrence exceptions are used to cancel specific virtualized occurrences
/// from a recurrence pattern without deleting the entire recurrence.
/// </remarks>
public interface IOccurrenceExceptionRepository
{
    /// <summary>
    /// Creates a new occurrence exception.
    /// </summary>
    /// <param name="exception">The exception to create.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created exception.</returns>
    Task<OccurrenceException> CreateAsync(
        OccurrenceException exception,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an exception by its identifier.
    /// </summary>
    /// <param name="id">The exception identifier.</param>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The exception if found; otherwise, null.</returns>
    Task<OccurrenceException?> GetByIdAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all exceptions for a specific recurrence.
    /// </summary>
    /// <param name="recurrenceId">The parent recurrence identifier.</param>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of exceptions for the recurrence.</returns>
    IAsyncEnumerable<OccurrenceException> GetByRecurrenceIdAsync(
        Guid recurrenceId,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all exceptions for recurrences that have occurrences in the specified date range.
    /// </summary>
    /// <param name="organization">The tenant identifier.</param>
    /// <param name="resourcePath">The resource path scope.</param>
    /// <param name="recurrenceIds">The recurrence identifiers to get exceptions for.</param>
    /// <param name="transactionContext">Optional transaction context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of matching exceptions.</returns>
    IAsyncEnumerable<OccurrenceException> GetByRecurrenceIdsAsync(
        string organization,
        string resourcePath,
        IEnumerable<Guid> recurrenceIds,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an exception.
    /// </summary>
    /// <param name="id">The exception identifier.</param>
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
    /// Deletes all exceptions for a specific recurrence.
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
