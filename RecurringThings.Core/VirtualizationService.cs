namespace RecurringThings.Core;

using System.Collections.Frozen;
using Domain;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NodaTime;
using Repository;
using Period = Ical.Net.DataTypes.Period;

/// <inheritdoc />
public class VirtualizationService(
    IRecurrenceRepository recurrenceRepository,
    IOccurrenceRepository occurrenceRepository) : IVirtualizationService
{
    private readonly IRecurrenceRepository _recurrenceRepository = recurrenceRepository ?? throw new ArgumentNullException(nameof(recurrenceRepository));
    private readonly IOccurrenceRepository _occurrenceRepository = occurrenceRepository ?? throw new ArgumentNullException(nameof(occurrenceRepository));

    /// <inheritdoc />
    public async Task<IReadOnlyList<VirtualizedOccurrence>> GetOccurrencesAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        if (startTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Kind must be UTC.", nameof(startTime));

        if (endTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Kind must be UTC.", nameof(endTime));

        var recurrencesTask = _recurrenceRepository.GetInRangeAsync(startTime, endTime, cancellationToken);
        var occurrencesTask = _occurrenceRepository.GetInRangeAsync(startTime, endTime, cancellationToken);

        var recurrences = await recurrencesTask;
        var occurrences = await occurrencesTask;

        var results = new List<VirtualizedOccurrence>(recurrences.Count * 4 + occurrences.Count);

        foreach (var recurrence in recurrences)
            results.AddRange(Virtualize(recurrence, startTime, endTime));

        results.AddRange(occurrences.Select(o => o.AsVirtualized()));

        return results.AsReadOnly();
    }

    /// <summary>
    ///     Virtualize a single recurrence configuration.
    /// </summary>
    /// <param name="recurrence"></param>
    /// <param name="rangeStartUtc"></param>
    /// <param name="rangeEndUtc"></param>
    /// <returns></returns>
    private static IEnumerable<VirtualizedOccurrence> Virtualize(
        Recurrence recurrence,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc)
    {
        var zone = DateTimeZoneProviders.Tzdb[recurrence.TimeZone];

        var recurrenceLocal = Instant.FromDateTimeUtc(recurrence.StartTime).InZone(zone).LocalDateTime;
        var rangeStartLocal = Instant.FromDateTimeUtc(rangeStartUtc).InZone(zone).LocalDateTime;
        var rangeEndLocal = Instant.FromDateTimeUtc(rangeEndUtc).InZone(zone).LocalDateTime;

        var recurrenceStart = new CalDateTime(recurrenceLocal.ToDateTimeUnspecified(), recurrence.TimeZone);
        var windowStart = new CalDateTime(rangeStartLocal.ToDateTimeUnspecified(), recurrence.TimeZone);
        var windowEnd = new CalDateTime(rangeEndLocal.ToDateTimeUnspecified(), recurrence.TimeZone);

        var calendarEvent = new CalendarEvent
        {
            Uid = recurrence.Id.ToString(),
            DtStart = recurrenceStart,
            Duration = recurrence.Duration,
            RecurrenceRules = [new RecurrencePattern(recurrence.RRule)]
        };

        if (recurrence.Exceptions.Count > 0)
        {
            var exceptions = new PeriodList();
            foreach (var ex in recurrence.Exceptions)
                exceptions.Add(new Period(new CalDateTime(ex.OriginalTime, recurrence.TimeZone)));

            calendarEvent.ExceptionDates.Add(exceptions);
        }

        var overrides = recurrence.Overrides.ToFrozenDictionary(x => x.OriginalTime.ToUniversalTime());

        return calendarEvent
            .GetOccurrences(windowStart, windowEnd)
            .Select(o =>
            {
                var utcStart = o.Period.StartTime.AsUtc;

                if (overrides.TryGetValue(utcStart, out var @override))
                    return @override.AsVirtualized();

                var v = recurrence.AsVirtualized();
                v.StartTime = o.Period.StartTime.AsUtc;
                v.Duration = o.Period.Duration;
                return v;
            })
            .Where(x =>
                x.StartTime >= rangeStartUtc &&
                x.StartTime < rangeEndUtc &&
                x.StartTime <= recurrence.RecurrenceEndTime);
    }
}
