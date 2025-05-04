namespace RecurringThings.Core;

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
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
    public async IAsyncEnumerable<VirtualizedOccurrence> GetOccurrencesAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (rangeStartUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Kind must be UTC.", nameof(rangeStartUtc));

        if (rangeEndUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Kind must be UTC.", nameof(rangeEndUtc));

        var recurrencesTask = _recurrenceRepository.GetInRangeAsync(rangeStartUtc, rangeEndUtc, cancellationToken);
        var occurrencesTask = _occurrenceRepository.GetInRangeAsync(rangeStartUtc, rangeEndUtc, cancellationToken);

        var recurrences = await recurrencesTask;
        var occurrences = await occurrencesTask;

        // return materialized recurrences.
        if (recurrences != null)
        {
            foreach (var recurrence in recurrences)
                foreach (var occurrence in Virtualize(recurrence, rangeStartUtc, rangeEndUtc))
                    yield return occurrence;
        }

        // return standalone occurrences.
        if (occurrences != null)
        {
            foreach (var occurrence in occurrences)
                if (IsInRange(occurrence, rangeStartUtc, rangeEndUtc))
                    yield return occurrence.AsVirtualized();
        }
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

        var recurrenceStartLocal = Instant.FromDateTimeUtc(recurrence.StartTime).InZone(zone).LocalDateTime;
        var rangeStartLocal = Instant.FromDateTimeUtc(rangeStartUtc).InZone(zone).LocalDateTime;
        var rangeEndLocal = Instant.FromDateTimeUtc(rangeEndUtc).InZone(zone).LocalDateTime;

        var recurrenceStart = new CalDateTime(recurrenceStartLocal.ToDateTimeUnspecified(), recurrence.TimeZone);
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
            .Where(x => IsInRange(x, rangeStartUtc, rangeEndUtc) &&
                        x.StartTime <= recurrence.RecurrenceEndTime);
    }

    /// <summary>
    ///     Gets whether the specified <see cref="ICalendarEntry"/> is within range, or not.
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="rangeStartUtc"></param>
    /// <param name="rangeEndUtc"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInRange(
        ICalendarEntry entry,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc)
    {
        return entry.StartTime >= rangeStartUtc &&
               entry.StartTime < rangeEndUtc;
    }

}
