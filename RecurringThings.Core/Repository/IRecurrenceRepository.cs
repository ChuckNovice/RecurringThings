namespace RecurringThings.Core.Repository;

using Domain;

/// <summary>
///     Provides access to persistence operations for recurrence definitions.
/// </summary>
public interface IRecurrenceRepository
{

    /// <summary>
    ///     Inserts a new recurrence into the data store.
    /// </summary>
    /// <param name="recurrence">
    ///     The domain recurrence object to insert.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to cancel the operation.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation.
    /// </returns>
    Task InsertAsync(Recurrence recurrence, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves all recurrences whose start time falls within the specified range.
    /// </summary>
    /// <param name="startTime">
    ///     The inclusive lower bound of the recurrence start time.
    /// </param>
    /// <param name="endTime">
    ///     The exclusive upper bound of the recurrence start time.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to cancel the operation.
    /// </param>
    /// <returns>
    ///     A task that returns a list of matching recurrence domain objects.
    /// </returns>
    Task<IReadOnlyList<Recurrence>> GetInRangeAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

}
