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
        if (recurrence == null) throw new ArgumentNullException(nameof(recurrence));

        if (recurrence.StartTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"Kind of the '{nameof(recurrence.StartTime)}' date must be UTC.", nameof(recurrence));

        if (recurrence.RecurrenceEndTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"Kind of the '{nameof(recurrence.RecurrenceEndTime)}' date must be UTC.", nameof(recurrence));

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
