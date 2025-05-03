namespace RecurringThings.Core;

using System.Collections.Frozen;
using Domain;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NodaTime;
using Repository;
using Occurrence = Domain.Occurrence;
using Period = Ical.Net.DataTypes.Period;

/// <inheritdoc />
public class VirtualizationService(
    IRecurrenceRepository recurrenceRepository,
    IOccurrenceRepository occurrenceRepository) :
    IVirtualizationService
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

        var recurrences = await _recurrenceRepository.GetInRangeAsync(startTime, endTime, cancellationToken);
        var occurrences = await _occurrenceRepository.GetInRangeAsync(startTime, endTime, cancellationToken);

        // virtualize each recurrence.
        var virtualizedTasks = recurrences
            .Select(r => Task.Run(() => Virtualize(r, startTime, endTime), cancellationToken))
            .ToList();
     
        var virtualizedOccurrences = await Task.WhenAll(virtualizedTasks);
        
        // concatenate the virtualized occurrences with the standalone occurrences.
        return virtualizedOccurrences
            .SelectMany(r => r)
            .Concat(
                occurrences
                    .Select(o => o.AsVirtualized()))
            .ToList();
    }

    /// <summary>
    ///     Virtualize a list of <see cref="Occurrence"/> for the specified <see cref="Recurrence"/>.
    /// </summary>
    /// <param name="recurrence"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <returns></returns>
    private static IReadOnlyList<VirtualizedOccurrence> Virtualize(
        Recurrence recurrence,
        DateTime startTime,
        DateTime endTime)
    {

        // Step 1: Use NodaTime to get the IANA timezone
        var dateTimeZone = DateTimeZoneProviders.Tzdb[recurrence.TimeZone];
        var recurrenceInstantStart = Instant.FromDateTimeUtc(recurrence.StartTime);
        var filterInstantStart = Instant.FromDateTimeUtc(startTime);
        var filterInstantEnd = Instant.FromDateTimeUtc(endTime);

        // Step 2: Convert to local time in the recurrence's time zone
        var recurrenceLocalStart = recurrenceInstantStart.InZone(dateTimeZone).LocalDateTime;
        var filterLocalStart = filterInstantStart.InZone(dateTimeZone).LocalDateTime;
        var filterLocalEnd = filterInstantEnd.InZone(dateTimeZone).LocalDateTime;

        // Step 3: Create CalDateTime with Unspecified kind + correct TimeZoneId
        var recurrenceStart = new CalDateTime(recurrenceLocalStart.ToDateTimeUnspecified(), recurrence.TimeZone);
        var filterStart = new CalDateTime(filterLocalStart.ToDateTimeUnspecified(), recurrence.TimeZone);
        var filterEnd = new CalDateTime(filterLocalEnd.ToDateTimeUnspecified(), recurrence.TimeZone);

        // configure the calendar and it's recurrence rules.
        var calendarEvent = new CalendarEvent
        {
            Uid = recurrence.Id.ToString(),
            DtStart = recurrenceStart,
            Duration = recurrence.Duration,
            RecurrenceRules = [new RecurrencePattern(recurrence.RRule)]
        };

        // configure the exceptions.
        if (recurrence.Exceptions.Count > 0)
        {
            var periodList = new PeriodList();
            foreach (var e in recurrence.Exceptions)
            {
                periodList.Add(new Period(new CalDateTime(e.OriginalTime, recurrence.TimeZone)));
            }
            calendarEvent.ExceptionDates.Add(periodList);
        }

        var overrides = recurrence.Overrides
            .ToFrozenDictionary(key => key.OriginalTime.ToUniversalTime());

        var recurrenceEndTime = recurrence.RecurrenceEndTime;

        // virtualize the occurrences.
        var virtualized = calendarEvent
            .GetOccurrences(filterStart, filterEnd)
            .Select(o =>
            {
                // override the occurrence.
                if (overrides.TryGetValue(o.Period.StartTime.Value.ToUniversalTime(), out var occurrenceOverride))
                    return occurrenceOverride.AsVirtualized();

                // generate a virtual occurrence based on the recurrence configuration.
                var result = recurrence.AsVirtualized();
                result.StartTime = o.Period.StartTime.AsUtc;
                result.Duration = o.Period.Duration;
                return result;
            })
            .Where(x => x.StartTime >= startTime && 
                        x.StartTime < endTime &&
                        x.StartTime <= recurrenceEndTime)
            .ToList();

        return virtualized;
    }

}
