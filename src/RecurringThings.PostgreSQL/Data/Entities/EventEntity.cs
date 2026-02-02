namespace RecurringThings.PostgreSQL.Data.Entities;

/// <summary>
/// PostgreSQL entity representing a persisted iCalendar component.
/// </summary>
internal sealed class EventEntity
{
    /// <summary>
    /// Gets or sets the auto-increment primary key.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier from the iCalendar component.
    /// </summary>
    public string Uid { get; set; } = null!;

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
    /// Gets or sets the serialized iCalendar data.
    /// </summary>
    public string SerializedData { get; set; } = null!;

    /// <summary>
    /// Gets or sets the categories associated with this event.
    /// </summary>
    public ICollection<CategoryEntity> Categories { get; set; } = [];

    /// <summary>
    /// Gets or sets the properties associated with this event.
    /// </summary>
    public ICollection<PropertyEntity> Properties { get; set; } = [];
}
