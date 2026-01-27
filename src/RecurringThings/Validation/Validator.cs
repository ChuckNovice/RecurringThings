namespace RecurringThings.Validation;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NodaTime;
using RecurringThings.Domain;
using RecurringThings.Models;

/// <summary>
/// Provides validation logic for RecurringThings entities and request models.
/// </summary>
public static partial class Validator
{
    private const int MaxOrganizationLength = 100;
    private const int MaxResourcePathLength = 100;
    private const int MinTypeLength = 1;
    private const int MaxTypeLength = 100;
    private const int MinRRuleLength = 1;
    private const int MaxRRuleLength = 2000;
    private const int MinTimeZoneLength = 1;
    private const int MaxTimeZoneLength = 100;
    private const int MinExtensionKeyLength = 1;
    private const int MaxExtensionKeyLength = 100;
    private const int MaxExtensionValueLength = 1024;

    /// <summary>
    /// Validates a <see cref="RecurrenceCreate"/> request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void Validate(RecurrenceCreate request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateOrganization(request.Organization);
        ValidateResourcePath(request.ResourcePath);
        ValidateType(request.Type);
        ValidateTimeZone(request.TimeZone);
        ValidateRRule(request.RRule, request.RecurrenceEndTimeUtc);
        ValidateDuration(request.Duration);
        ValidateUtcDateTime(request.StartTimeUtc, nameof(request.StartTimeUtc));
        ValidateUtcDateTime(request.RecurrenceEndTimeUtc, nameof(request.RecurrenceEndTimeUtc));
        ValidateExtensions(request.Extensions);
    }

    /// <summary>
    /// Validates an <see cref="OccurrenceCreate"/> request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void Validate(OccurrenceCreate request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateOrganization(request.Organization);
        ValidateResourcePath(request.ResourcePath);
        ValidateType(request.Type);
        ValidateTimeZone(request.TimeZone);
        ValidateDuration(request.Duration);
        ValidateUtcDateTime(request.StartTimeUtc, nameof(request.StartTimeUtc));
        ValidateExtensions(request.Extensions);
    }

    /// <summary>
    /// Validates an <see cref="OccurrenceException"/> entity.
    /// </summary>
    /// <param name="exception">The exception entity to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void Validate(OccurrenceException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        ValidateOrganization(exception.Organization);
        ValidateResourcePath(exception.ResourcePath);
        ValidateUtcDateTime(exception.OriginalTimeUtc, nameof(exception.OriginalTimeUtc));
        ValidateExtensions(exception.Extensions);
    }

    /// <summary>
    /// Validates an <see cref="OccurrenceOverride"/> entity.
    /// </summary>
    /// <param name="override">The override entity to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="override"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void Validate(OccurrenceOverride @override)
    {
        ArgumentNullException.ThrowIfNull(@override);

        ValidateOrganization(@override.Organization);
        ValidateResourcePath(@override.ResourcePath);
        ValidateDuration(@override.Duration);
        ValidateUtcDateTime(@override.OriginalTimeUtc, nameof(@override.OriginalTimeUtc));
        ValidateUtcDateTime(@override.StartTime, nameof(@override.StartTime));
        ValidateExtensions(@override.Extensions);
        ValidateExtensions(@override.OriginalExtensions);
    }

    /// <summary>
    /// Validates that an exception or override belongs to the same tenant scope as its parent recurrence.
    /// </summary>
    /// <param name="parentRecurrence">The parent recurrence.</param>
    /// <param name="childOrganization">The child entity's organization.</param>
    /// <param name="childResourcePath">The child entity's resource path.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the child entity has a different Organization or ResourcePath than the parent.
    /// </exception>
    public static void ValidateTenantScope(
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
    /// Validates an Organization field value.
    /// </summary>
    /// <param name="organization">The organization value to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="organization"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void ValidateOrganization(string organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        if (organization.Length > MaxOrganizationLength)
        {
            throw new ArgumentException(
                $"Organization must not exceed {MaxOrganizationLength} characters. Actual length: {organization.Length}.",
                nameof(organization));
        }
    }

    /// <summary>
    /// Validates a ResourcePath field value.
    /// </summary>
    /// <param name="resourcePath">The resource path value to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void ValidateResourcePath(string resourcePath)
    {
        ArgumentNullException.ThrowIfNull(resourcePath);

        if (resourcePath.Length > MaxResourcePathLength)
        {
            throw new ArgumentException(
                $"ResourcePath must not exceed {MaxResourcePathLength} characters. Actual length: {resourcePath.Length}.",
                nameof(resourcePath));
        }
    }

    /// <summary>
    /// Validates a Type field value.
    /// </summary>
    /// <param name="type">The type value to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void ValidateType(string type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.Length < MinTypeLength)
        {
            throw new ArgumentException(
                $"Type must be at least {MinTypeLength} character(s).",
                nameof(type));
        }

        if (type.Length > MaxTypeLength)
        {
            throw new ArgumentException(
                $"Type must not exceed {MaxTypeLength} characters. Actual length: {type.Length}.",
                nameof(type));
        }
    }

    /// <summary>
    /// Validates a TimeZone field value.
    /// </summary>
    /// <param name="timeZone">The time zone identifier to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeZone"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails or the time zone is invalid.</exception>
    public static void ValidateTimeZone(string timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        if (timeZone.Length < MinTimeZoneLength)
        {
            throw new ArgumentException(
                $"TimeZone must be at least {MinTimeZoneLength} character(s).",
                nameof(timeZone));
        }

        if (timeZone.Length > MaxTimeZoneLength)
        {
            throw new ArgumentException(
                $"TimeZone must not exceed {MaxTimeZoneLength} characters. Actual length: {timeZone.Length}.",
                nameof(timeZone));
        }

        // Validate that it's a valid IANA time zone
        var provider = DateTimeZoneProviders.Tzdb;
        if (provider.GetZoneOrNull(timeZone) is null)
        {
            throw new ArgumentException(
                $"TimeZone '{timeZone}' is not a valid IANA time zone identifier.",
                nameof(timeZone));
        }
    }

    /// <summary>
    /// Validates an RRule string.
    /// </summary>
    /// <param name="rrule">The RRule to validate.</param>
    /// <param name="recurrenceEndTimeUtc">The expected recurrence end time that must match UNTIL.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rrule"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void ValidateRRule(string rrule, DateTime recurrenceEndTimeUtc)
    {
        ArgumentNullException.ThrowIfNull(rrule);

        if (rrule.Length < MinRRuleLength)
        {
            throw new ArgumentException(
                $"RRule must be at least {MinRRuleLength} character(s).",
                nameof(rrule));
        }

        if (rrule.Length > MaxRRuleLength)
        {
            throw new ArgumentException(
                $"RRule must not exceed {MaxRRuleLength} characters. Actual length: {rrule.Length}.",
                nameof(rrule));
        }

        // Check for COUNT (not supported)
        if (CountRegex().IsMatch(rrule))
        {
            throw new ArgumentException(
                "RRule COUNT is not supported. Use UNTIL instead.",
                nameof(rrule));
        }

        // Check for UNTIL (required)
        var untilMatch = UntilRegex().Match(rrule);
        if (!untilMatch.Success)
        {
            throw new ArgumentException(
                "RRule must contain UNTIL. COUNT is not supported.",
                nameof(rrule));
        }

        var untilValue = untilMatch.Groups["until"].Value;

        // UNTIL must be in UTC (end with Z)
        if (!untilValue.EndsWith("Z", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"RRule UNTIL must be in UTC (must end with 'Z'). Found: {untilValue}",
                nameof(rrule));
        }

        // Parse UNTIL and compare with recurrenceEndTimeUtc
        if (!TryParseICalDateTime(untilValue, out var parsedUntil))
        {
            throw new ArgumentException(
                $"RRule UNTIL value '{untilValue}' is not a valid iCalendar date-time.",
                nameof(rrule));
        }

        // Allow 1 second tolerance for comparison due to potential rounding
        var difference = Math.Abs((parsedUntil - recurrenceEndTimeUtc).TotalSeconds);
        if (difference > 1)
        {
            throw new ArgumentException(
                $"RecurrenceEndTimeUtc ({recurrenceEndTimeUtc:O}) must match RRule UNTIL ({untilValue}).",
                nameof(rrule));
        }
    }

    /// <summary>
    /// Validates a Duration value.
    /// </summary>
    /// <param name="duration">The duration to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the duration is not positive.</exception>
    public static void ValidateDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Duration must be positive.",
                nameof(duration));
        }
    }

    /// <summary>
    /// Validates that a DateTime is in UTC.
    /// </summary>
    /// <param name="dateTime">The DateTime to validate.</param>
    /// <param name="parameterName">The name of the parameter for the exception.</param>
    /// <exception cref="ArgumentException">Thrown when the DateTime is not UTC.</exception>
    public static void ValidateUtcDateTime(DateTime dateTime, string parameterName)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"{parameterName} must be in UTC (Kind must be DateTimeKind.Utc). Actual Kind: {dateTime.Kind}.",
                parameterName);
        }
    }

    /// <summary>
    /// Validates an Extensions dictionary.
    /// </summary>
    /// <param name="extensions">The extensions dictionary to validate. Null is allowed.</param>
    /// <exception cref="ArgumentException">Thrown when key/value constraints are violated.</exception>
    public static void ValidateExtensions(Dictionary<string, string>? extensions)
    {
        if (extensions is null)
        {
            return;
        }

        foreach (var (key, value) in extensions)
        {
            if (key is null)
            {
                throw new ArgumentException(
                    "Extension keys cannot be null.",
                    nameof(extensions));
            }

            if (key.Length < MinExtensionKeyLength)
            {
                throw new ArgumentException(
                    $"Extension keys must be at least {MinExtensionKeyLength} character(s). Found empty key.",
                    nameof(extensions));
            }

            if (key.Length > MaxExtensionKeyLength)
            {
                throw new ArgumentException(
                    $"Extension keys must not exceed {MaxExtensionKeyLength} characters. Key '{key}' has length {key.Length}.",
                    nameof(extensions));
            }

            if (value is null)
            {
                throw new ArgumentException(
                    $"Extension values cannot be null. Key: '{key}'.",
                    nameof(extensions));
            }

            if (value.Length > MaxExtensionValueLength)
            {
                throw new ArgumentException(
                    $"Extension values must not exceed {MaxExtensionValueLength} characters. Key '{key}' has value length {value.Length}.",
                    nameof(extensions));
            }
        }
    }

    private static bool TryParseICalDateTime(string value, out DateTime result)
    {
        result = default;

        // Remove 'Z' suffix for parsing
        var toParse = value.TrimEnd('Z');

        // iCalendar format: YYYYMMDDTHHMMSS
        if (toParse.Length != 15 || toParse[8] != 'T')
        {
            return false;
        }

        if (!int.TryParse(toParse.AsSpan(0, 4), out var year) ||
            !int.TryParse(toParse.AsSpan(4, 2), out var month) ||
            !int.TryParse(toParse.AsSpan(6, 2), out var day) ||
            !int.TryParse(toParse.AsSpan(9, 2), out var hour) ||
            !int.TryParse(toParse.AsSpan(11, 2), out var minute) ||
            !int.TryParse(toParse.AsSpan(13, 2), out var second))
        {
            return false;
        }

        try
        {
            result = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"(?:^|;)COUNT\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex CountRegex();

    [GeneratedRegex(@"(?:^|;)UNTIL\s*=\s*(?<until>[^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UntilRegex();
}
