namespace RecurringThings.Validation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ical.Net.DataTypes;
using NodaTime;
using RecurringThings.Domain;
using RecurringThings.Exceptions;
using RecurringThings.Options;

/// <summary>
/// Provides validation logic for RecurringThings entities and request models.
/// </summary>
public static partial class Validator
{
    /// <summary>
    /// Validates that an exception or override belongs to the same tenant scope as its parent recurrence.
    /// </summary>
    /// <param name="parentRecurrence">The parent recurrence.</param>
    /// <param name="childOrganization">The child entity's organization.</param>
    /// <param name="childResourcePath">The child entity's resource path.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the child entity has a different Organization or ResourcePath than the parent.
    /// </exception>
    internal static void ValidateTenantScope(
        Recurrence parentRecurrence,
        string childOrganization,
        string childResourcePath)
    {
        ArgumentNullException.ThrowIfNull(parentRecurrence);

        if (!string.Equals(parentRecurrence.Organization, childOrganization, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Organization mismatch: exception/override Organization '{childOrganization}' " +
                $"must match parent recurrence Organization '{parentRecurrence.Organization}'.");
        }

        if (!string.Equals(parentRecurrence.ResourcePath, childResourcePath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ResourcePath mismatch: exception/override ResourcePath '{childResourcePath}' " +
                $"must match parent recurrence ResourcePath '{parentRecurrence.ResourcePath}'.");
        }
    }

    /// <summary>
    /// Validates that the types filter is not empty.
    /// </summary>
    /// <param name="types">The types array to validate.</param>
    /// <exception cref="ArgumentException">Thrown when types is an empty array.</exception>
    public static void ValidateTypesFilter(string[]? types)
    {
        if (types is { Length: 0 })
        {
            throw new ArgumentException("Types filter cannot be an empty array. Use null to include all types.", nameof(types));
        }
    }

    /// <summary>
    /// Validates parameters for creating a recurrence.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any parameter is invalid.</exception>
    public static void ValidateRecurrenceCreate(
        string organization,
        string resourcePath,
        string type,
        DateTime startTime,
        TimeSpan duration,
        string rrule,
        string timeZone,
        Dictionary<string, string>? extensions)
    {
        ValidateOrganization(organization);
        ValidateResourcePath(resourcePath);
        ValidateType(type);
        ValidateStartTime(startTime);
        ValidateDuration(duration);
        ValidateRRule(rrule);
        ValidateTimeZone(timeZone);
        ValidateExtensions(extensions);
    }

    /// <summary>
    /// Validates parameters for creating an occurrence.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any parameter is invalid.</exception>
    public static void ValidateOccurrenceCreate(
        string organization,
        string resourcePath,
        string type,
        DateTime startTime,
        TimeSpan duration,
        string timeZone,
        Dictionary<string, string>? extensions)
    {
        ValidateOrganization(organization);
        ValidateResourcePath(resourcePath);
        ValidateType(type);
        ValidateStartTime(startTime);
        ValidateDuration(duration);
        ValidateTimeZone(timeZone);
        ValidateExtensions(extensions);
    }

    /// <summary>
    /// Validates the organization parameter.
    /// </summary>
    private static void ValidateOrganization(string organization)
    {
        ArgumentNullException.ThrowIfNull(organization, nameof(organization));

        if (organization.Length > ValidationConstants.MaxOrganizationLength)
        {
            throw new ArgumentException(
                $"Organization must not exceed {ValidationConstants.MaxOrganizationLength} characters. Actual length: {organization.Length}.",
                nameof(organization));
        }
    }

    /// <summary>
    /// Validates the resource path parameter.
    /// </summary>
    private static void ValidateResourcePath(string resourcePath)
    {
        ArgumentNullException.ThrowIfNull(resourcePath, nameof(resourcePath));

        if (resourcePath.Length > ValidationConstants.MaxResourcePathLength)
        {
            throw new ArgumentException(
                $"ResourcePath must not exceed {ValidationConstants.MaxResourcePathLength} characters. Actual length: {resourcePath.Length}.",
                nameof(resourcePath));
        }
    }

    /// <summary>
    /// Validates the type parameter.
    /// </summary>
    private static void ValidateType(string type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (type.Length < ValidationConstants.MinTypeLength)
        {
            throw new ArgumentException(
                $"Type must be at least {ValidationConstants.MinTypeLength} character(s).",
                nameof(type));
        }

        if (type.Length > ValidationConstants.MaxTypeLength)
        {
            throw new ArgumentException(
                $"Type must not exceed {ValidationConstants.MaxTypeLength} characters. Actual length: {type.Length}.",
                nameof(type));
        }
    }

    /// <summary>
    /// Validates the start time parameter.
    /// </summary>
    private static void ValidateStartTime(DateTime startTime)
    {
        if (startTime.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                "StartTime must have a specified Kind (Utc or Local). DateTimeKind.Unspecified is not allowed.",
                nameof(startTime));
        }
    }

    /// <summary>
    /// Validates the duration parameter.
    /// </summary>
    private static void ValidateDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Duration must be positive.", nameof(duration));
        }
    }

    /// <summary>
    /// Validates the RRule parameter.
    /// </summary>
    private static void ValidateRRule(string rrule)
    {
        ArgumentNullException.ThrowIfNull(rrule, nameof(rrule));

        if (rrule.Length < ValidationConstants.MinRRuleLength)
        {
            throw new ArgumentException(
                $"RRule must be at least {ValidationConstants.MinRRuleLength} character(s).",
                nameof(rrule));
        }

        if (rrule.Length > ValidationConstants.MaxRRuleLength)
        {
            throw new ArgumentException(
                $"RRule must not exceed {ValidationConstants.MaxRRuleLength} characters. Actual length: {rrule.Length}.",
                nameof(rrule));
        }

        // Validate that RRule can be parsed
        RecurrencePattern pattern;
        try
        {
            pattern = new RecurrencePattern(rrule);
        }
        catch
        {
            throw new ArgumentException("RRule is not a valid RFC 5545 recurrence rule.", nameof(rrule));
        }

        // Validate that COUNT is not used
        if (pattern.Count.HasValue)
        {
            throw new ArgumentException("RRule COUNT is not supported. Use UNTIL instead.", nameof(rrule));
        }

        // Validate that UNTIL is present
        if (pattern.Until is null)
        {
            throw new ArgumentException("RRule must contain UNTIL. COUNT is not supported.", nameof(rrule));
        }

        // Validate that UNTIL is in UTC (ends with Z)
        var untilMatch = UntilRegex().Match(rrule);
        if (untilMatch.Success)
        {
            var untilValue = untilMatch.Groups["until"].Value;
            if (!untilValue.EndsWith('Z'))
            {
                throw new ArgumentException(
                    $"RRule UNTIL must be in UTC (must end with 'Z'). Found: {untilValue}",
                    nameof(rrule));
            }
        }
    }

    /// <summary>
    /// Validates the time zone parameter.
    /// </summary>
    private static void ValidateTimeZone(string timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone, nameof(timeZone));

        if (timeZone.Length < ValidationConstants.MinTimeZoneLength)
        {
            throw new ArgumentException(
                $"TimeZone must be at least {ValidationConstants.MinTimeZoneLength} character(s).",
                nameof(timeZone));
        }

        if (timeZone.Length > ValidationConstants.MaxTimeZoneLength)
        {
            throw new ArgumentException(
                $"TimeZone must not exceed {ValidationConstants.MaxTimeZoneLength} characters. Actual length: {timeZone.Length}.",
                nameof(timeZone));
        }

        if (DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZone) is null)
        {
            throw new ArgumentException(
                $"TimeZone '{timeZone}' is not a valid IANA time zone identifier.",
                nameof(timeZone));
        }
    }

    /// <summary>
    /// Validates the extensions dictionary.
    /// </summary>
    private static void ValidateExtensions(Dictionary<string, string>? extensions)
    {
        if (extensions is null)
        {
            return;
        }

        foreach (var (key, value) in extensions)
        {
            if (key is null)
            {
                throw new ArgumentException("Extension keys cannot be null.", nameof(extensions));
            }

            if (key.Length < ValidationConstants.MinExtensionKeyLength)
            {
                throw new ArgumentException(
                    $"Extension keys must be at least {ValidationConstants.MinExtensionKeyLength} character(s). Found empty key.",
                    nameof(extensions));
            }

            if (key.Length > ValidationConstants.MaxExtensionKeyLength)
            {
                throw new ArgumentException(
                    $"Extension keys must not exceed {ValidationConstants.MaxExtensionKeyLength} characters. Key '{key}' has length {key.Length}.",
                    nameof(extensions));
            }

            if (value is null)
            {
                throw new ArgumentException($"Extension values cannot be null. Key: '{key}'.", nameof(extensions));
            }

            if (value.Length > ValidationConstants.MaxExtensionValueLength)
            {
                throw new ArgumentException(
                    $"Extension values must not exceed {ValidationConstants.MaxExtensionValueLength} characters. Key '{key}' has value length {value.Length}.",
                    nameof(extensions));
            }
        }
    }

    [GeneratedRegex(@"(?:^|;)UNTIL\s*=\s*(?<until>[^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UntilRegex();

    /// <summary>
    /// Validates monthly recurrence day bounds and returns affected months if any.
    /// </summary>
    /// <param name="pattern">The parsed recurrence pattern.</param>
    /// <param name="startTime">The recurrence start time.</param>
    /// <returns>List of affected month numbers (1-12), or empty if no issues.</returns>
    /// <remarks>
    /// <para>
    /// Returns an empty list when:
    /// </para>
    /// <list type="bullet">
    /// <item>Pattern is not MONTHLY frequency</item>
    /// <item>All BYMONTHDAY values are &lt;= 28 (safe for all months)</item>
    /// <item>No months in the recurrence range are affected</item>
    /// </list>
    /// </remarks>
    internal static IReadOnlyList<int> GetAffectedMonthsForMonthlyPattern(
        RecurrencePattern pattern,
        DateTime startTime)
    {
        // Only validate for MONTHLY frequency
        if (pattern.Frequency != Ical.Net.FrequencyType.Monthly)
        {
            return [];
        }

        // Get BYMONTHDAY values (if empty, defaults to start day)
        var byMonthDays = pattern.ByMonthDay.Count > 0
            ? pattern.ByMonthDay.ToList()
            : [startTime.Day];

        // If all days are <= 28, no month will have issues
        if (byMonthDays.All(d => d <= 28))
        {
            return [];
        }

        // Get the recurrence end date from UNTIL (IDateTime -> DateTime)
        var until = pattern.Until;
        if (until is null)
        {
            return [];
        }

        var untilDate = until.Value;

        // Find affected months in the range
        var affectedMonths = new HashSet<int>();
        var current = new DateTime(startTime.Year, startTime.Month, 1);

        while (current <= untilDate)
        {
            var daysInMonth = DateTime.DaysInMonth(current.Year, current.Month);

            foreach (var day in byMonthDays)
            {
                if (day > daysInMonth)
                {
                    affectedMonths.Add(current.Month);
                }
            }

            current = current.AddMonths(1);
        }

        return affectedMonths.Order().ToList();
    }

    /// <summary>
    /// Validates monthly day bounds and throws or returns the effective strategy.
    /// </summary>
    /// <param name="pattern">The parsed recurrence pattern.</param>
    /// <param name="startTime">The recurrence start time.</param>
    /// <param name="options">The creation options (optional).</param>
    /// <returns>The strategy to use, or null if no out-of-bounds handling needed.</returns>
    /// <exception cref="MonthDayOutOfBoundsException">
    /// Thrown when strategy is <see cref="MonthDayOutOfBoundsStrategy.Throw"/> and affected months exist.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Returns null when:
    /// </para>
    /// <list type="bullet">
    /// <item>Pattern is not MONTHLY (daily, weekly, yearly patterns don't need this)</item>
    /// <item>All BYMONTHDAY values are &lt;= 28 (safe for all months)</item>
    /// <item>No affected months exist in the recurrence range</item>
    /// </list>
    /// <para>
    /// This ensures we only store the strategy when it's actually needed,
    /// avoiding garbage data on non-monthly recurrences.
    /// </para>
    /// </remarks>
    internal static MonthDayOutOfBoundsStrategy? ValidateMonthlyDayBounds(
        RecurrencePattern pattern,
        DateTime startTime,
        CreateRecurrenceOptions? options)
    {
        var affectedMonths = GetAffectedMonthsForMonthlyPattern(pattern, startTime);

        // Return null for non-monthly patterns or when no months are affected
        // This ensures we don't store unnecessary data
        if (affectedMonths.Count == 0)
        {
            return null;
        }

        var strategy = options?.OutOfBoundsMonthBehavior ?? MonthDayOutOfBoundsStrategy.Throw;

        if (strategy == MonthDayOutOfBoundsStrategy.Throw)
        {
            var byMonthDays = pattern.ByMonthDay.Count > 0
                ? pattern.ByMonthDay.ToList()
                : [startTime.Day];

            throw new MonthDayOutOfBoundsException(byMonthDays.Max(), affectedMonths);
        }

        return strategy;
    }
}
