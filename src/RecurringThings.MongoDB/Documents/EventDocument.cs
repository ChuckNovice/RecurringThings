namespace RecurringThings.MongoDB.Documents;

/// <summary>
/// MongoDB document representing a persisted iCalendar component.
/// </summary>
internal sealed class EventDocument
{
    /// <summary>
    /// Gets or sets the unique identifier (maps to entry.Uid, serves as MongoDB _id).
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the component type (Event, Todo, or Journal).
    /// </summary>
    public ComponentType ComponentType { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier for multi-tenant isolation. Can be empty string.
    /// </summary>
    public string TenantId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional user identifier.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the denormalized start date for efficient querying.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the denormalized end date for efficient querying.
    /// Null indicates an infinite recurrence.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the categories from the iCalendar component.
    /// </summary>
    public List<string> Categories { get; set; } = [];

    /// <summary>
    /// Gets or sets the properties from the iCalendar component.
    /// </summary>
    public List<PropertyDocument> Properties { get; set; } = [];

    /// <summary>
    /// Gets or sets the serialized iCalendar data.
    /// </summary>
    public string SerializedData { get; set; } = null!;
}

/// <summary>
/// MongoDB embedded document representing an iCalendar property.
/// </summary>
internal sealed class PropertyDocument
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the property value. Can be null to represent properties with no value.
    /// </summary>
    public string? Value { get; set; }
}
