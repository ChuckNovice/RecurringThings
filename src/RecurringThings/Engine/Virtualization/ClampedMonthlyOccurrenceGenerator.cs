namespace RecurringThings.Engine.Virtualization;

using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net.DataTypes;
using NodaTime;
using RecurringThings.Domain;

/// <summary>
/// Generates occurrences for monthly patterns with Clamp strategy.
/// </summary>
/// <remarks>
/// <para>
/// This generator proactively iterates through all months in the recurrence range
/// and clamps out-of-bounds days to the last day of each month.
/// </para>
/// <para>
/// For example, a recurrence set to the 31st of each month will generate:
/// </para>
/// <list type="bullet">
/// <item>January 31st (31 days in month)</item>
/// <item>February 28th/29th (clamped from 31)</item>
/// <item>March 31st (31 days in month)</item>
/// <item>April 30th (clamped from 31)</item>
/// </list>
/// <para>
/// Unlike detecting skipped months after Ical.Net evaluation, this approach correctly
/// handles edge cases like when the last month of the recurrence would be skipped.
/// </para>
/// <para>
/// <b>Important:</b> Clamped occurrences respect the UNTIL date. If UNTIL is March 15
/// and BYMONTHDAY is 30, no March occurrence is generated because March 30 exceeds UNTIL.
/// </para>
/// </remarks>
internal sealed class ClampedMonthlyOccurrenceGenerator : IOccurrenceGenerator
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

        // Get the target day from BYMONTHDAY or from the recurrence start
        var targetDay = pattern.ByMonthDay.Count > 0
            ? pattern.ByMonthDay.Max()
            : GetLocalDay(recurrence.StartTime, timeZone);

        // Get the time-of-day from the recurrence start (in local time)
        var recurrenceStartInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(recurrence.StartTime, DateTimeKind.Utc));
        var recurrenceStartLocal = recurrenceStartInstant.InZone(timeZone).LocalDateTime;
        var timeOfDay = recurrenceStartLocal.TimeOfDay;

        // Convert UNTIL to local time for month iteration
        var untilInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(recurrence.RecurrenceEndTime, DateTimeKind.Utc));
        var untilLocal = untilInstant.InZone(timeZone).LocalDateTime;

        // Start from the first month of the recurrence
        var currentMonth = new LocalDate(recurrenceStartLocal.Year, recurrenceStartLocal.Month, 1);
        var untilDate = untilLocal.Date;

        while (currentMonth <= untilDate)
        {
            // Calculate the actual day (clamped to the number of days in the month)
            var daysInMonth = currentMonth.Calendar.GetDaysInMonth(currentMonth.Year, currentMonth.Month);
            var actualDay = Math.Min(targetDay, daysInMonth);

            // Build the occurrence local date time
            var occurrenceLocal = new LocalDateTime(
                currentMonth.Year,
                currentMonth.Month,
                actualDay,
                timeOfDay.Hour,
                timeOfDay.Minute,
                timeOfDay.Second);

            // Convert to UTC
            var zonedDateTime = occurrenceLocal.InZoneLeniently(timeZone);
            var utcDateTime = zonedDateTime.ToDateTimeUtc();

            // Check: occurrence must not exceed the recurrence end time
            if (utcDateTime > recurrence.RecurrenceEndTime)
            {
                // If clamped occurrence exceeds UNTIL, skip this month
                // (e.g., UNTIL=March 15, BYMONTHDAY=30 → no March occurrence)
                currentMonth = currentMonth.PlusMonths(1);
                continue;
            }

            // Check: occurrence must be at or after the recurrence start
            if (utcDateTime < recurrence.StartTime)
            {
                currentMonth = currentMonth.PlusMonths(1);
                continue;
            }

            // Check: occurrence must be within query range
            if (utcDateTime >= queryStartUtc && utcDateTime <= queryEndUtc)
            {
                yield return utcDateTime;
            }

            currentMonth = currentMonth.PlusMonths(1);
        }
    }

    /// <summary>
    /// Gets the day of month from a UTC DateTime in the specified timezone.
    /// </summary>
    private static int GetLocalDay(DateTime utcDateTime, DateTimeZone timeZone)
    {
        var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc));
        var local = instant.InZone(timeZone).LocalDateTime;
        return local.Day;
    }
}
