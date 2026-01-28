namespace RecurringThings.Engine.Virtualization;

using RecurringThings.Domain;
using RecurringThings.Options;

/// <summary>
/// Creates the appropriate occurrence generator based on recurrence configuration.
/// </summary>
/// <remarks>
/// <para>
/// This factory uses a singleton pattern for generators since they are stateless.
/// The appropriate generator is selected based on the recurrence's
/// <see cref="Recurrence.MonthDayBehavior"/> property.
/// </para>
/// <para>
/// Generator selection:
/// </para>
/// <list type="bullet">
/// <item><see cref="MonthDayOutOfBoundsStrategy.Clamp"/> → <see cref="ClampedMonthlyOccurrenceGenerator"/></item>
/// <item>All other cases → <see cref="IcalNetOccurrenceGenerator"/></item>
/// </list>
/// <para>
/// The default <see cref="IcalNetOccurrenceGenerator"/> handles:
/// </para>
/// <list type="bullet">
/// <item>Non-monthly patterns (MonthDayBehavior is null)</item>
/// <item>Monthly patterns without out-of-bounds issues (MonthDayBehavior is null)</item>
/// <item><see cref="MonthDayOutOfBoundsStrategy.Skip"/> strategy (Ical.Net naturally skips invalid dates)</item>
/// </list>
/// </remarks>
internal static class OccurrenceGeneratorFactory
{
    private static readonly IcalNetOccurrenceGenerator IcalNetGenerator = new();
    private static readonly ClampedMonthlyOccurrenceGenerator ClampedGenerator = new();

    /// <summary>
    /// Gets the appropriate occurrence generator for the specified recurrence.
    /// </summary>
    /// <param name="recurrence">The recurrence to generate occurrences for.</param>
    /// <returns>An occurrence generator appropriate for the recurrence's configuration.</returns>
    /// <remarks>
    /// <para>
    /// Uses <see cref="ClampedMonthlyOccurrenceGenerator"/> only when
    /// <see cref="Recurrence.MonthDayBehavior"/> is explicitly set to
    /// <see cref="MonthDayOutOfBoundsStrategy.Clamp"/>.
    /// </para>
    /// <para>
    /// For all other cases (null, Skip), uses <see cref="IcalNetOccurrenceGenerator"/>.
    /// </para>
    /// </remarks>
    public static IOccurrenceGenerator GetGenerator(Recurrence recurrence)
    {
        return recurrence.MonthDayBehavior == MonthDayOutOfBoundsStrategy.Clamp
            ? ClampedGenerator
            : IcalNetGenerator;
    }
}
