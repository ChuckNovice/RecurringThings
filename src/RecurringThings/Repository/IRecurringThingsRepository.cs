namespace RecurringThings.Repository;

using RecurringThings.Filters;

/// <summary>
/// Repository interface for RecurringThings persistence.
/// </summary>
internal interface IRecurringThingsRepository
{
    /// <summary>
    /// Gets entries from the repository matching the specified filter criteria.
    /// </summary>
    /// <param name="tenantId">Tenant identifier for multi-tenant isolation. Can be empty string.</param>
    /// <param name="filter">The filter specifying query criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of serialized iCalendar strings.</returns>
    IAsyncEnumerable<string> GetAsync(
        string tenantId,
        EventFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new entry in the repository.
    /// </summary>
    /// <param name="metadata">The event metadata containing all entry information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(
        EventMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entry in the repository.
    /// </summary>
    /// <param name="metadata">The event metadata containing all entry information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the update was performed, false if no matching record was found.</returns>
    Task<bool> UpdateAsync(
        EventMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing entry from the repository.
    /// </summary>
    /// <param name="uid">The unique identifier of the entry.</param>
    /// <param name="tenantId">Tenant identifier for multi-tenant isolation.</param>
    /// <param name="userId">Optional user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the delete was performed, false if no matching record was found.</returns>
    Task<bool> DeleteAsync(
        string uid,
        string tenantId,
        string? userId,
        CancellationToken cancellationToken = default);
}
