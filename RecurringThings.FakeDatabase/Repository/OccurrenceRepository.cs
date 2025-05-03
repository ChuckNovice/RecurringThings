namespace RecurringThings.FakeDatabase.Repository;

using System.Collections.Concurrent;
using Core.Domain;
using RecurringThings.Core.Repository;

/// <inheritdoc />
public class OccurrenceRepository :
    IOccurrenceRepository
{

    private readonly ConcurrentDictionary<Guid, Occurrence> _data = [];

    /// <inheritdoc />
    public Task InsertAsync(Occurrence occurrence, CancellationToken cancellationToken = default)
    {
        if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));

        if (occurrence.StartTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"Kind of the '{nameof(occurrence.StartTime)}' date must be UTC.", nameof(occurrence));

        _data.TryAdd(occurrence.Id, occurrence);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Occurrence>> GetInRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (endTime < startTime)
            throw new ArgumentException($"The '{nameof(endTime)}' date must be greater or equal to '{nameof(startTime)}'.", nameof(endTime));

        var result = _data
            .Values
            .Where(r =>
                r.StartTime < endTime && 
                r.StartTime + r.Duration > startTime)
            .ToList();

        return Task.FromResult((IReadOnlyList<Occurrence>)result);
    }
}
