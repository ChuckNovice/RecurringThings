namespace RecurringThings.PostgreSQL.Data.Entities;

/// <summary>
/// PostgreSQL entity representing a category associated with an event.
/// </summary>
internal sealed class CategoryEntity
{
    /// <summary>
    /// Gets or sets the auto-increment primary key.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the parent event.
    /// </summary>
    public long EventId { get; set; }

    /// <summary>
    /// Gets or sets the category value.
    /// </summary>
    public string Value { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the parent event.
    /// </summary>
    public EventEntity Event { get; set; } = null!;
}
