namespace RecurringThings.FakeDatabase.Repository;

using System.Collections.Concurrent;
using Core.Domain;
using RecurringThings.Core.Repository;

/// <inheritdoc />
public class RecurrenceRepository :
    IRecurrenceRepository
{

    private readonly ConcurrentDictionary<Guid, Recurrence> _data = [];

    /// <inheritdoc />
    public Task InsertAsync(Recurrence recurrence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        // In a real database, either validate that the passed object uses UTC for the start time of the recurrence,
        // or at least ensure conversion to UTC when possible.
        if (recurrence.StartTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"Kind of the '{nameof(recurrence.StartTime)}' date must be UTC.", nameof(recurrence));

        // In a real database, either validate that the passed object uses UTC for the end time of the recurrence,
        // or at least ensure conversion to UTC when possible.
        if (recurrence.RecurrenceEndTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"Kind of the '{nameof(recurrence.RecurrenceEndTime)}' date must be UTC.", nameof(recurrence));

        // In a real database, either validate that the passed object uses UTC for the original time of all the overrides,
        // or at least ensure conversion to UTC when possible.
        if (recurrence.Overrides.Any(x => x.OriginalTime.Kind != DateTimeKind.Utc))
            throw new ArgumentException($"Kind of the '{nameof(OccurrenceOverride)}.{nameof(OccurrenceOverride.OriginalTime)}' date must all be UTC.", nameof(recurrence));

        // In a real database, either validate that the passed object uses UTC for the start time of all the overrides,
        // or at least ensure conversion to UTC when possible.
        if (recurrence.Overrides.Any(x => x.StartTime.Kind != DateTimeKind.Utc))
            throw new ArgumentException($"Kind of the '{nameof(OccurrenceOverride)}.{nameof(OccurrenceOverride.StartTime)}' date must all be UTC.", nameof(recurrence));

        // In a real database, either validate that the passed object uses UTC for the original time of all the exceptions,
        // or at least ensure conversion to UTC when possible.
        if (recurrence.Exceptions.Any(x => x.OriginalTime.Kind != DateTimeKind.Utc))
            throw new ArgumentException($"Kind of the '{nameof(OccurrenceException)}.{nameof(OccurrenceException.OriginalTime)}' date must all be UTC.", nameof(recurrence));

        // The end time of the recurrence should never be less than the start time.
        if (recurrence.RecurrenceEndTime < recurrence.StartTime)
            throw new ArgumentException(
                $"The '{nameof(recurrence.RecurrenceEndTime)}' date must be greater or equal to '{nameof(recurrence.StartTime)}'.", nameof(recurrence));

        _data.TryAdd(recurrence.Id, recurrence);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Recurrence>> GetInRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (endTime < startTime)
            throw new ArgumentException($"The '{nameof(endTime)}' date must be greater or equal to '{nameof(startTime)}'.", nameof(endTime));

        var result = _data.Values
            .Where(r =>
                r.StartTime < endTime &&
                r.RecurrenceEndTime > startTime)
            .ToList()
            .AsReadOnly();

        return Task.FromResult((IReadOnlyList<Recurrence>)result);
    }
}
