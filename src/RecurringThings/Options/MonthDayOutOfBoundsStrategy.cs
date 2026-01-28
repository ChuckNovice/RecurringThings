namespace RecurringThings.Options;

/// <summary>
/// Specifies how to handle monthly recurrences where the specified day
/// doesn't exist in all months within the recurrence range.
/// </summary>
/// <remarks>
/// <para>
/// When creating a monthly recurrence with a day that doesn't exist in all months
/// (e.g., 31st doesn't exist in April, June, September, November, or February),
/// this strategy determines the behavior.
/// </para>
/// <para>
/// The default strategy is <see cref="Throw"/>, which allows the caller to
/// prompt the user for a choice before proceeding.
/// </para>
/// </remarks>
public enum MonthDayOutOfBoundsStrategy
{
    /// <summary>
    /// Throws a <see cref="Exceptions.MonthDayOutOfBoundsException"/> when the day falls
    /// out of bounds for at least one month within the recurrence range.
    /// </summary>
    /// <remarks>
    /// This is the default behavior, allowing the caller to prompt the user for a choice
    /// before calling <c>CreateRecurrenceAsync</c> again with <see cref="Skip"/> or <see cref="Clamp"/>.
    /// </remarks>
    Throw = 0,

    /// <summary>
    /// Skips months where the specified day doesn't exist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For example, a recurrence on the 31st will skip April, June, September, November,
    /// and February (which have fewer than 31 days).
    /// </para>
    /// <para>
    /// This results in fewer occurrences than months in the range.
    /// </para>
    /// </remarks>
    Skip = 1,

    /// <summary>
    /// Automatically selects the last day of the month when the specified day
    /// exceeds the number of days in that month.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For example, if the recurrence is set to the 31st:
    /// <list type="bullet">
    /// <item>April (30 days) → 30th</item>
    /// <item>February (28/29 days) → 28th or 29th</item>
    /// <item>June (30 days) → 30th</item>
    /// </list>
    /// </para>
    /// <para>
    /// This ensures every month in the range has exactly one occurrence.
    /// </para>
    /// </remarks>
    Clamp = 2
}
