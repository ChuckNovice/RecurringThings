namespace RecurringThings.Options;

/// <summary>
/// Options for creating a recurrence pattern.
/// </summary>
/// <remarks>
/// Pass this to <c>CreateRecurrenceAsync</c> to customize recurrence creation behavior.
/// </remarks>
public class CreateRecurrenceOptions
{
    /// <summary>
    /// Gets or sets the strategy for handling monthly recurrences where the specified day
    /// doesn't exist in all months within the recurrence range.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <see cref="MonthDayOutOfBoundsStrategy.Throw"/>, which throws an exception
    /// to allow the caller to prompt the user for a choice.
    /// </para>
    /// <para>
    /// This option only applies to monthly recurrences (FREQ=MONTHLY) with BYMONTHDAY values
    /// greater than 28. For other frequencies or day values, this option has no effect.
    /// </para>
    /// </remarks>
    public MonthDayOutOfBoundsStrategy OutOfBoundsMonthBehavior { get; set; }
        = MonthDayOutOfBoundsStrategy.Throw;
}
