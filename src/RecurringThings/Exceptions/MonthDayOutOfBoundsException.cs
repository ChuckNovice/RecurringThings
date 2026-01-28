namespace RecurringThings.Exceptions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Exception thrown when a monthly recurrence specifies a day that doesn't exist
/// in at least one month within the recurrence range.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when creating a monthly recurrence with the default
/// <see cref="Options.MonthDayOutOfBoundsStrategy.Throw"/> strategy, and the specified
/// BYMONTHDAY value doesn't exist in all months within the recurrence range.
/// </para>
/// <para>
/// The caller should handle this exception by prompting the user to choose between
/// <see cref="Options.MonthDayOutOfBoundsStrategy.Skip"/> (skip months where the day doesn't exist)
/// or <see cref="Options.MonthDayOutOfBoundsStrategy.Clamp"/> (use the last day of the month).
/// </para>
/// </remarks>
public class MonthDayOutOfBoundsException : Exception
{
    /// <summary>
    /// Gets the day of month that caused the out-of-bounds condition.
    /// </summary>
    /// <remarks>
    /// This is the BYMONTHDAY value from the RRule, or the day from StartTime if BYMONTHDAY wasn't specified.
    /// </remarks>
    public int DayOfMonth { get; }

    /// <summary>
    /// Gets the months (1-12) where the specified day doesn't exist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Month numbers follow the standard convention: 1 = January, 2 = February, etc.
    /// </para>
    /// <para>
    /// For example, if <see cref="DayOfMonth"/> is 31, this list would contain
    /// 2 (February), 4 (April), 6 (June), 9 (September), and 11 (November).
    /// </para>
    /// </remarks>
    public IReadOnlyList<int> AffectedMonths { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonthDayOutOfBoundsException"/> class.
    /// </summary>
    /// <param name="dayOfMonth">The day of month that doesn't exist in all months.</param>
    /// <param name="affectedMonths">The list of month numbers (1-12) where the day doesn't exist.</param>
    public MonthDayOutOfBoundsException(int dayOfMonth, IReadOnlyList<int> affectedMonths)
        : base(FormatMessage(dayOfMonth, affectedMonths))
    {
        DayOfMonth = dayOfMonth;
        AffectedMonths = affectedMonths;
    }

    private static string FormatMessage(int dayOfMonth, IReadOnlyList<int> affectedMonths)
    {
        var monthNames = FormatMonthNames(affectedMonths);
        return $"Monthly recurrence day {dayOfMonth} doesn't exist in all months. " +
               $"Affected months: {monthNames}. " +
               "Consider using Skip or Clamp strategy.";
    }

    private static string FormatMonthNames(IReadOnlyList<int> months)
    {
        return string.Join(", ", months.Select(m =>
            CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)));
    }
}
