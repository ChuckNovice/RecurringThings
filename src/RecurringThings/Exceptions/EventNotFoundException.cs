namespace RecurringThings.Exceptions;

/// <summary>
/// Exception thrown when an event is not found.
/// </summary>
public sealed class EventNotFoundException : Exception
{
    /// <summary>
    /// Gets the UID of the event that was not found.
    /// </summary>
    public string Uid { get; }

    /// <summary>
    /// Gets the tenant ID used in the search.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the user ID used in the search.
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventNotFoundException"/> class.
    /// </summary>
    /// <param name="uid">The UID of the event that was not found.</param>
    /// <param name="tenantId">The tenant ID used in the search.</param>
    /// <param name="userId">The user ID used in the search.</param>
    public EventNotFoundException(string uid, string tenantId, string? userId)
        : base($"Event with UID '{uid}' not found for tenant '{tenantId}' and user '{userId ?? "(null)"}'.")
    {
        Uid = uid;
        TenantId = tenantId;
        UserId = userId;
    }
}
