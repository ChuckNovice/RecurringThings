namespace RecurringThings.Repository;

/// <summary>
/// Contains metadata for creating or updating an event in the repository.
/// </summary>
/// <param name="Uid">The unique identifier of the entry.</param>
/// <param name="ComponentType">The type of component (Event, Todo, or Journal).</param>
/// <param name="TenantId">Tenant identifier for multi-tenant isolation. Can be empty string.</param>
/// <param name="UserId">Optional user identifier.</param>
/// <param name="StartDate">Denormalized start date for efficient querying.</param>
/// <param name="EndDate">Denormalized end date for efficient querying, or null if infinite.</param>
/// <param name="Categories">The categories extracted from the entry.</param>
/// <param name="Properties">The properties extracted from the entry as name-value pairs. Values can be null.</param>
/// <param name="SerializedData">The serialized iCalendar data.</param>
internal record EventMetadata(
    string Uid,
    ComponentType ComponentType,
    string TenantId,
    string? UserId,
    DateTime? StartDate,
    DateTime? EndDate,
    IReadOnlyList<string> Categories,
    Dictionary<string, string?> Properties,
    string SerializedData);
