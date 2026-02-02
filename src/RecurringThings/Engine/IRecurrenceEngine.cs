namespace RecurringThings.Engine;

using Ical.Net.CalendarComponents;
using RecurringThings.Exceptions;
using RecurringThings.Filters;

/// <summary>
/// Defines the contract for the recurrence engine.
/// </summary>
public interface IRecurrenceEngine
{
    /// <summary>
    /// Creates a new event from an iCalendar recurring component.
    /// </summary>
    /// <param name="entry">The iCalendar recurring component (e.g., CalendarEvent, Todo).</param>
    /// <param name="tenantId">Tenant identifier for multi-tenant isolation. Can be empty string.</param>
    /// <param name="userId">Optional user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateEventAsync(
        IRecurringComponent entry,
        string tenantId,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing event from an iCalendar recurring component.
    /// </summary>
    /// <param name="entry">The iCalendar recurring component (e.g., CalendarEvent, Todo).</param>
    /// <param name="tenantId">Tenant identifier for multi-tenant isolation. Can be empty string.</param>
    /// <param name="userId">Optional user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="EventNotFoundException">Thrown when no matching event is found.</exception>
    Task UpdateEventAsync(
        IRecurringComponent entry,
        string tenantId,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing event.
    /// </summary>
    /// <param name="uid">The unique identifier of the event.</param>
    /// <param name="tenantId">Tenant identifier for multi-tenant isolation. Can be empty string.</param>
    /// <param name="userId">Optional user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="EventNotFoundException">Thrown when no matching event is found.</exception>
    Task DeleteEventAsync(
        string uid,
        string tenantId,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events matching the specified filter criteria as a streaming async enumerable.
    /// </summary>
    /// <param name="tenantId">Tenant identifier for multi-tenant isolation. Can be empty string.</param>
    /// <param name="filter">The filter specifying query criteria. Use <see cref="EventFilter.Create"/> to build.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of deserialized iCalendar recurring components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filter is null.</exception>
    IAsyncEnumerable<IRecurringComponent> GetEventsAsync(
        string tenantId,
        EventFilter filter,
        CancellationToken cancellationToken = default);
}
