namespace RecurringThings.Core.Repository;

using Domain;

/// <summary>
///     Provides access to persistence operations for occurrence definitions.
/// </summary>
public interface IOccurrenceRepository
{

    /// <summary>
    ///     Inserts a new occurrence into the data store.
    /// </summary>
    /// <param name="occurrence">
    ///     The domain occurrence object to insert.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to cancel the operation.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation.
    /// </returns>
    Task InsertAsync(Occurrence occurrence, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves all occurrences whose start time falls within the specified range.
    /// </summary>
    /// <param name="startTime">
    ///     The inclusive lower bound of the occurrence start time.
    /// </param>
    /// <param name="endTime">
    ///     The exclusive upper bound of the occurrence start time.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to cancel the operation.
    /// </param>
    /// <returns>
    ///     A task that returns a list of matching occurrence domain objects.
    /// </returns>
    Task<IReadOnlyList<Occurrence>> GetInRangeAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

}
