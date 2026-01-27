namespace RecurringThings.Validation.Validators;

using System;
using System.Text.RegularExpressions;
using FluentValidation;
using NodaTime;
using RecurringThings.Models;

/// <summary>
/// FluentValidation validator for <see cref="RecurrenceCreate"/> requests.
/// </summary>
internal sealed partial class RecurrenceCreateValidator : AbstractValidator<RecurrenceCreate>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurrenceCreateValidator"/> class.
    /// </summary>
    public RecurrenceCreateValidator()
    {
        RuleFor(x => x.Organization)
            .NotNull()
            .WithMessage("Organization cannot be null.")
            .MaximumLength(ValidationConstants.MaxOrganizationLength)
            .WithMessage($"Organization must not exceed {ValidationConstants.MaxOrganizationLength} characters. Actual length: {{TotalLength}}.");

        RuleFor(x => x.ResourcePath)
            .NotNull()
            .WithMessage("ResourcePath cannot be null.")
            .MaximumLength(ValidationConstants.MaxResourcePathLength)
            .WithMessage($"ResourcePath must not exceed {ValidationConstants.MaxResourcePathLength} characters. Actual length: {{TotalLength}}.");

        RuleFor(x => x.Type)
            .NotNull()
            .WithMessage("Type cannot be null.")
            .MinimumLength(ValidationConstants.MinTypeLength)
            .WithMessage($"Type must be at least {ValidationConstants.MinTypeLength} character(s).")
            .MaximumLength(ValidationConstants.MaxTypeLength)
            .WithMessage($"Type must not exceed {ValidationConstants.MaxTypeLength} characters. Actual length: {{TotalLength}}.");

        RuleFor(x => x.TimeZone)
            .NotNull()
            .WithMessage("TimeZone cannot be null.")
            .MinimumLength(ValidationConstants.MinTimeZoneLength)
            .WithMessage($"TimeZone must be at least {ValidationConstants.MinTimeZoneLength} character(s).")
            .MaximumLength(ValidationConstants.MaxTimeZoneLength)
            .WithMessage($"TimeZone must not exceed {ValidationConstants.MaxTimeZoneLength} characters. Actual length: {{TotalLength}}.")
            .MustBeValidIanaTimeZone();

        RuleFor(x => x.RRule)
            .NotNull()
            .WithMessage("RRule cannot be null.")
            .MinimumLength(ValidationConstants.MinRRuleLength)
            .WithMessage($"RRule must be at least {ValidationConstants.MinRRuleLength} character(s).")
            .MaximumLength(ValidationConstants.MaxRRuleLength)
            .WithMessage($"RRule must not exceed {ValidationConstants.MaxRRuleLength} characters. Actual length: {{TotalLength}}.")
            .Must(rrule => !CountRegex().IsMatch(rrule))
            .WithMessage("RRule COUNT is not supported. Use UNTIL instead.")
            .Must(rrule => UntilRegex().IsMatch(rrule))
            .WithMessage("RRule must contain UNTIL. COUNT is not supported.")
            .Must(HasUtcUntil)
            .WithMessage(GetUntilNotUtcMessage);

        // Cross-property validation: RRule UNTIL must match RecurrenceEndTime when converted to UTC
        RuleFor(x => x.RRule)
            .Must((request, rrule) => ValidateUntilMatchesEndTime(rrule, request.RecurrenceEndTime, request.TimeZone))
            .WithMessage(x => $"RecurrenceEndTime ({x.RecurrenceEndTime:O}) must match RRule UNTIL when converted to UTC.");

        RuleFor(x => x.StartTime)
            .MustNotBeUnspecified();

        RuleFor(x => x.RecurrenceEndTime)
            .MustNotBeUnspecified();

        RuleFor(x => x.Duration)
            .MustBePositive();

        RuleFor(x => x.Extensions)
            .ValidExtensions();
    }

    private static bool HasUtcUntil(string rrule)
    {
        var untilMatch = UntilRegex().Match(rrule);
        if (!untilMatch.Success)
        {
            return true; // Will be caught by the UNTIL required rule
        }

        var untilValue = untilMatch.Groups["until"].Value;
        return untilValue.EndsWith('Z');
    }

    private static string GetUntilNotUtcMessage(RecurrenceCreate request)
    {
        var untilMatch = UntilRegex().Match(request.RRule);
        if (!untilMatch.Success)
        {
            return "RRule must contain UNTIL.";
        }

        var untilValue = untilMatch.Groups["until"].Value;
        return $"RRule UNTIL must be in UTC (must end with 'Z'). Found: {untilValue}";
    }

    private static bool ValidateUntilMatchesEndTime(string rrule, DateTime recurrenceEndTime, string? timeZone)
    {
        var untilMatch = UntilRegex().Match(rrule);
        if (!untilMatch.Success)
        {
            return true; // Will be caught by the UNTIL required rule
        }

        var untilValue = untilMatch.Groups["until"].Value;

        if (!TryParseICalDateTime(untilValue, out var parsedUntil))
        {
            return false;
        }

        // Convert recurrenceEndTime to UTC if it's Local
        var endTimeUtc = recurrenceEndTime;
        if (recurrenceEndTime.Kind == DateTimeKind.Local && !string.IsNullOrEmpty(timeZone))
        {
            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZone);
            if (tz is not null)
            {
                var localDateTime = LocalDateTime.FromDateTime(recurrenceEndTime);
                var zonedDateTime = localDateTime.InZoneLeniently(tz);
                endTimeUtc = zonedDateTime.ToDateTimeUtc();
            }
        }

        // Allow 1 second tolerance for comparison due to potential rounding
        var difference = Math.Abs((parsedUntil - endTimeUtc).TotalSeconds);
        return difference <= 1;
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
