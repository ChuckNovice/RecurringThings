namespace RecurringThings.Engine.Virtualization;

using System;
using System.Collections.Generic;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NodaTime;
using RecurringThings.Domain;

/// <summary>
/// Generates occurrences using Ical.Net's RRule evaluation.
/// </summary>
/// <remarks>
/// <para>
/// This generator uses the standard Ical.Net library to evaluate recurrence patterns.
/// It is used for:
/// </para>
/// <list type="bullet">
/// <item>Non-monthly patterns (daily, weekly, yearly)</item>
/// <item>Monthly patterns without out-of-bounds issues</item>
/// <item>Monthly patterns with <see cref="Options.MonthDayOutOfBoundsStrategy.Skip"/> strategy</item>
/// </list>
/// <para>
/// Ical.Net naturally skips dates that don't exist (e.g., February 30th), which implements
/// the Skip behavior for out-of-bounds monthly days.
/// </para>
/// </remarks>
internal sealed class IcalNetOccurrenceGenerator : IOccurrenceGenerator
{
    /// <inheritdoc/>
    public IEnumerable<DateTime> GenerateOccurrences(
        Recurrence recurrence,
        RecurrencePattern pattern,
        DateTime queryStartUtc,
        DateTime queryEndUtc)
    {
        // Get the IANA timezone
        var timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(recurrence.TimeZone);
        if (timeZone is null)
        {
            yield break;
        }

        // Convert query range to local time
        var startInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(queryStartUtc, DateTimeKind.Utc));
        var endInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(queryEndUtc, DateTimeKind.Utc));

        var startLocal = startInstant.InZone(timeZone).LocalDateTime;
        var endLocal = endInstant.InZone(timeZone).LocalDateTime;

        // Convert recurrence start time to local
        var recurrenceStartInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(recurrence.StartTime, DateTimeKind.Utc));
        var recurrenceStartLocal = recurrenceStartInstant.InZone(timeZone).LocalDateTime;

        // Create a calendar event with the RRule
        var calendar = new Calendar();
        var calendarEvent = new CalendarEvent
        {
            DtStart = new CalDateTime(
                recurrenceStartLocal.Year,
                recurrenceStartLocal.Month,
                recurrenceStartLocal.Day,
                recurrenceStartLocal.Hour,
                recurrenceStartLocal.Minute,
                recurrenceStartLocal.Second,
                recurrence.TimeZone)
        };

        // Add the pre-parsed pattern
        calendarEvent.RecurrenceRules.Add(pattern);

        calendar.Events.Add(calendarEvent);

        // Get occurrences in the local time range
        var searchStart = new CalDateTime(startLocal.Year, startLocal.Month, startLocal.Day, 0, 0, 0, recurrence.TimeZone);
        var searchEnd = new CalDateTime(endLocal.Year, endLocal.Month, endLocal.Day, 23, 59, 59, recurrence.TimeZone);

        var icalOccurrences = calendarEvent.GetOccurrences(searchStart).TakeWhileBefore(searchEnd);

        foreach (var icalOccurrence in icalOccurrences)
        {
            var occurrenceDateTime = icalOccurrence.Period.StartTime;

            // Convert back to UTC using NodaTime for correct DST handling
            var localDateTime = new LocalDateTime(
                occurrenceDateTime.Year,
                occurrenceDateTime.Month,
                occurrenceDateTime.Day,
                occurrenceDateTime.Hour,
                occurrenceDateTime.Minute,
                occurrenceDateTime.Second);

            // Handle ambiguous times during DST fall-back
            var zonedDateTime = localDateTime.InZoneLeniently(timeZone);
            var utcDateTime = zonedDateTime.ToDateTimeUtc();

            // Filter to exact query range
            if (utcDateTime >= queryStartUtc && utcDateTime <= queryEndUtc)
            {
                // Also check against RecurrenceEndTime
                if (utcDateTime <= recurrence.RecurrenceEndTime)
                {
                    yield return utcDateTime;
                }
            }
        }
    }
}
