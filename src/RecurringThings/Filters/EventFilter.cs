namespace RecurringThings.Filters;

using RecurringThings.Extensions;

/// <summary>
/// User filter mode for event queries.
/// </summary>
public enum UserFilterMode
{
    /// <summary>Return all events regardless of user assignment.</summary>
    All,

    /// <summary>Return events for a specific user ID.</summary>
    Specific,

    /// <summary>Return only tenant-wide events (userId is null).</summary>
    TenantWide
}

/// <summary>
/// Immutable filter for querying recurring events.
/// Use <see cref="Create"/> to start building a filter.
/// </summary>
public sealed record EventFilter
{
    /// <summary>
    /// Gets the user filter mode.
    /// </summary>
    public UserFilterMode UserMode { get; }

    /// <summary>
    /// Gets the specific user ID when <see cref="UserMode"/> is <see cref="UserFilterMode.Specific"/>.
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Gets the optional start of date range (already converted to UTC).
    /// </summary>
    public DateTime? StartDateUtc { get; }

    /// <summary>
    /// Gets the optional end of date range (already converted to UTC).
    /// </summary>
    public DateTime? EndDateUtc { get; }

    /// <summary>
    /// Gets the optional component type filter.
    /// </summary>
    public ComponentType? ComponentType { get; }

    /// <summary>
    /// Gets the optional categories filter (lowercase, matches any, case-insensitive).
    /// </summary>
    public IReadOnlyList<string>? Categories { get; }

    internal EventFilter(
        UserFilterMode userMode,
        string? userId,
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        ComponentType? componentType,
        IReadOnlyList<string>? categories)
    {
        UserMode = userMode;
        UserId = userId;
        StartDateUtc = startDateUtc;
        EndDateUtc = endDateUtc;
        ComponentType = componentType;
        Categories = categories;
    }

    /// <summary>
    /// Creates a new filter builder.
    /// </summary>
    /// <returns>A new <see cref="EventFilterBuilder"/> instance.</returns>
    public static EventFilterBuilder Create() => new();
}

/// <summary>
/// Fluent builder for creating <see cref="EventFilter"/> instances.
/// </summary>
public sealed class EventFilterBuilder
{
    private UserFilterMode _userMode = UserFilterMode.All;
    private string? _userId;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private ComponentType? _componentType;
    private string[]? _categories;

    internal EventFilterBuilder() { }

    /// <summary>
    /// Includes all events regardless of user assignment. This is the default.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public EventFilterBuilder AllUsers()
    {
        _userMode = UserFilterMode.All;
        _userId = null;
        return this;
    }

    /// <summary>
    /// Filters to events for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to filter by.</param>
    /// <returns>This builder for chaining.</returns>
    public EventFilterBuilder ForUser(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        _userMode = UserFilterMode.Specific;
        _userId = userId;
        return this;
    }

    /// <summary>
    /// Filters to only tenant-wide events (events with no user assigned).
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public EventFilterBuilder TenantWideOnly()
    {
        _userMode = UserFilterMode.TenantWide;
        _userId = null;
        return this;
    }

    /// <summary>
    /// Sets the start of the date range filter.
    /// </summary>
    /// <param name="startDate">The start date (UTC or Local, not Unspecified).</param>
    /// <returns>This builder for chaining.</returns>
    public EventFilterBuilder From(DateTime? startDate)
    {
        _startDate = startDate;
        return this;
    }

    /// <summary>
    /// Sets the end of the date range filter.
    /// </summary>
    /// <param name="endDate">The end date (UTC or Local, not Unspecified).</param>
    /// <returns>This builder for chaining.</returns>
    public EventFilterBuilder To(DateTime? endDate)
    {
        _endDate = endDate;
        return this;
    }

    /// <summary>
    /// Sets both start and end of the date range filter.
    /// </summary>
    /// <param name="startDate">The start date (UTC or Local, not Unspecified).</param>
    /// <param name="endDate">The end date (UTC or Local, not Unspecified).</param>
    /// <returns>This builder for chaining.</returns>
    public EventFilterBuilder Between(DateTime? startDate, DateTime? endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
        return this;
    }

    /// <summary>
    /// Filters to a specific component type.
    /// </summary>
    /// <param name="componentType">The component type to filter by.</param>
    /// <returns>This builder for chaining.</returns>
    public EventFilterBuilder OfType(ComponentType componentType)
    {
        _componentType = componentType;
        return this;
    }

    /// <summary>
    /// Filters to events that have any of the specified categories.
    /// </summary>
    /// <param name="categories">The categories to filter by (case-insensitive, matches any).</param>
    /// <returns>This builder for chaining.</returns>
    public EventFilterBuilder InCategories(params string[] categories)
    {
        _categories = categories.Length > 0 ? categories : null;
        return this;
    }

    /// <summary>
    /// Builds the filter with validation.
    /// </summary>
    /// <returns>An immutable <see cref="EventFilter"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when dates have <see cref="DateTimeKind.Unspecified"/> or categories contain duplicates.
    /// </exception>
    public EventFilter Build()
    {
        // Validate DateTime.Kind - reject Unspecified
        if (_startDate.HasValue && _startDate.Value.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                $"StartDate must have a {nameof(DateTimeKind)} of {nameof(DateTimeKind.Utc)} or {nameof(DateTimeKind.Local)}, not {nameof(DateTimeKind.Unspecified)}.",
                "startDate");
        }

        if (_endDate.HasValue && _endDate.Value.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                $"EndDate must have a {nameof(DateTimeKind)} of {nameof(DateTimeKind.Utc)} or {nameof(DateTimeKind.Local)}, not {nameof(DateTimeKind.Unspecified)}.",
                "endDate");
        }

        // Validate no duplicate categories (case-insensitive)
        if (_categories is { Length: > 0 } && _categories.HasDuplicate(StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Categories must contain unique values (case-insensitive).", "categories");
        }

        // Convert dates to UTC
        var startDateUtc = _startDate?.ToUniversalTime();
        var endDateUtc = _endDate?.ToUniversalTime();

        // Normalize categories to lowercase
        var normalizedCategories = _categories?.Select(c => c.ToLowerInvariant()).ToList();

        return new EventFilter(
            _userMode,
            _userId,
            startDateUtc,
            endDateUtc,
            _componentType,
            normalizedCategories);
    }
}
