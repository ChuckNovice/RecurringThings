namespace RecurringThings.Engine;

using System.Runtime.CompilerServices;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;
using RecurringThings.Exceptions;
using RecurringThings.Extensions;
using RecurringThings.Filters;
using RecurringThings.Repository;

/// <summary>
/// Default implementation of <see cref="IRecurrenceEngine"/>.
/// </summary>
internal sealed class RecurrenceEngine : IRecurrenceEngine
{
    private readonly IRecurringThingsRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurrenceEngine"/> class.
    /// </summary>
    /// <param name="repository">The repository for all RecurringThings entities.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is null.</exception>
    public RecurrenceEngine(IRecurringThingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IRecurringComponent> GetEventsAsync(
        string tenantId,
        EventFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // All validation (DateTime.Kind, duplicate categories) is done at EventFilter.Build() time.
        // The filter already contains UTC-converted dates and lowercase categories.

        await foreach (var serializedData in _repository.GetAsync(
            tenantId,
            filter,
            cancellationToken))
        {
            var calendar = Calendar.Load(serializedData);
            if (calendar is null)
            {
                continue;
            }

            // Yield all components from the deserialized calendar.
            // ComponentType filtering is already done at the repository level.
            foreach (var evt in calendar.Events)
            {
                yield return evt;
            }

            foreach (var todo in calendar.Todos)
            {
                yield return todo;
            }

            foreach (var journal in calendar.Journals)
            {
                yield return journal;
            }
        }
    }

    /// <inheritdoc />
    public Task CreateEventAsync(
        IRecurringComponent entry,
        string tenantId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var metadata = ValidateAndBuildMetadata(entry, tenantId, userId);
        return _repository.CreateAsync(metadata, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateEventAsync(
        IRecurringComponent entry,
        string tenantId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var metadata = ValidateAndBuildMetadata(entry, tenantId, userId);
        var updated = await _repository.UpdateAsync(metadata, cancellationToken);

        if (!updated)
        {
            throw new EventNotFoundException(entry.Uid!, tenantId, userId);
        }
    }

    /// <inheritdoc />
    public async Task DeleteEventAsync(
        string uid,
        string tenantId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uid);

        var deleted = await _repository.DeleteAsync(uid, tenantId, userId, cancellationToken);

        if (!deleted)
        {
            throw new EventNotFoundException(uid, tenantId, userId);
        }
    }

    /// <summary>
    /// Validates an entry and builds the event metadata for repository operations.
    /// </summary>
    /// <param name="entry">The entry to validate.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The optional user identifier.</param>
    /// <returns>The populated event metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entry is null.</exception>
    /// <exception cref="ArgumentException">Thrown when entry is invalid.</exception>
    private static EventMetadata ValidateAndBuildMetadata(IRecurringComponent entry, string tenantId, string? userId)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Uid);

        var componentType = entry switch
        {
            CalendarEvent => ComponentType.Event,
            Todo => ComponentType.Todo,
            Journal => ComponentType.Journal,
            _ => throw new ArgumentException(
                $"Entry must be a {nameof(CalendarEvent)}, {nameof(Todo)}, or {nameof(Journal)}. Got: {entry.GetType().Name}",
                nameof(entry))
        };

        // Validate no duplicate categories (case-insensitive).
        // Use AsEnumerable() to avoid Ical.Net's GroupedList index-based enumeration issues.
        if (entry.Categories.AsEnumerable().HasDuplicate(StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{nameof(entry.Categories)} must contain unique values.", nameof(entry));
        }

        // Validate no duplicate property names (case-insensitive).
        if (entry.Properties.AsEnumerable().Select(p => p.Name).HasDuplicate(StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{nameof(entry.Properties)} must contain unique names.", nameof(entry));
        }

        // Validate property names are not null or whitespace.
        if (entry.Properties.AsEnumerable().Any(p => string.IsNullOrWhiteSpace(p.Name)))
        {
            throw new ArgumentException($"{nameof(entry.Properties)} must not contain null or whitespace names.", nameof(entry));
        }

        return new EventMetadata(
            entry.Uid,
            componentType,
            tenantId,
            userId,
            entry.Start?.AsUtc,
            GetEndDate(entry),
            [.. entry.Categories.Select(x => x.ToLowerInvariant())],
            entry.Properties.ToDictionary(p => p.Name.ToLowerInvariant(), p => p.Value?.ToString()),
            SerializeEntry(entry, componentType));
    }

    /// <summary>
    /// Serializes the entry to iCalendar format.
    /// </summary>
    private static string SerializeEntry(IRecurringComponent entry, ComponentType componentType)
    {
        var calendar = new Calendar();

        switch (componentType)
        {
            case ComponentType.Event:
                calendar.Events.Add((CalendarEvent)entry);
                break;
            case ComponentType.Todo:
                calendar.Todos.Add((Todo)entry);
                break;
            case ComponentType.Journal:
                calendar.Journals.Add((Journal)entry);
                break;
        }

        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(calendar) ?? string.Empty;
    }

    /// <summary>
    /// Determines the end date of a recurring component for query denormalization.
    /// </summary>
    /// <param name="entry">The recurring component to analyze.</param>
    /// <returns>
    /// The effective end time of the last occurrence in UTC, or <c>null</c> if the
    /// recurrence is infinite (any rule has neither UNTIL nor COUNT specified).
    /// </returns>
    private static DateTime? GetEndDate(IRecurringComponent entry)
    {
        // Journal components don't implement occurrence evaluation
        if (entry is Journal)
            return null;

        // If there are recurrence rules, check if any is infinite (no UNTIL and no COUNT)
        if (entry.RecurrenceRules is { Count: > 0 })
        {
            if (entry.RecurrenceRules.Any(rule => rule.Count is null && rule.Until is null))
                return null;
        }

        // All rules are finite (have UNTIL or COUNT), compute the last occurrence
        var lastOccurrence = entry.GetOccurrences().LastOrDefault();
        // Use AsUtc to properly handle timezone conversion
        return lastOccurrence?.Period.EffectiveEndTime?.AsUtc;
    }

}
