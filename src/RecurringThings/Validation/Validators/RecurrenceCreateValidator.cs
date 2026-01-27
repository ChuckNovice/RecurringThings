namespace RecurringThings.Validation.Validators;

using System.Text.RegularExpressions;
using FluentValidation;
using Ical.Net.DataTypes;
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
            .Must(BeValidRRule)
            .WithMessage("RRule is not a valid RFC 5545 recurrence rule.")
            .Must(MustNotHaveCount)
            .WithMessage("RRule COUNT is not supported. Use UNTIL instead.")
            .Must(MustHaveUntil)
            .WithMessage("RRule must contain UNTIL. COUNT is not supported.")
            .Must(HasUtcUntil)
            .WithMessage(GetUntilNotUtcMessage);

        RuleFor(x => x.StartTime)
            .MustNotBeUnspecified();

        RuleFor(x => x.Duration)
            .MustBePositive();

        RuleFor(x => x.Extensions)
            .ValidExtensions();
    }

    /// <summary>
    /// Validates that the RRule can be parsed by Ical.Net.
    /// </summary>
    private static bool BeValidRRule(string rrule)
    {
        if (string.IsNullOrEmpty(rrule))
        {
            return true; // Will be caught by NotNull/MinimumLength rules
        }

        try
        {
            _ = new RecurrencePattern(rrule);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that the RRule does not use COUNT.
    /// </summary>
    private static bool MustNotHaveCount(string rrule)
    {
        if (string.IsNullOrEmpty(rrule))
        {
            return true; // Will be caught by NotNull/MinimumLength rules
        }

        try
        {
            var pattern = new RecurrencePattern(rrule);

            // COUNT must not be specified (null when not set in Ical.Net)
            return !pattern.Count.HasValue;
        }
        catch
        {
            return true; // Will be caught by BeValidRRule
        }
    }

    /// <summary>
    /// Validates that the RRule contains UNTIL.
    /// </summary>
    private static bool MustHaveUntil(string rrule)
    {
        if (string.IsNullOrEmpty(rrule))
        {
            return true; // Will be caught by NotNull/MinimumLength rules
        }

        try
        {
            var pattern = new RecurrencePattern(rrule);

            // UNTIL must be specified (null when not set)
            return pattern.Until is not null;
        }
        catch
        {
            return true; // Will be caught by BeValidRRule
        }
    }

    /// <summary>
    /// Validates that the UNTIL value in the RRule is in UTC (ends with Z).
    /// </summary>
    private static bool HasUtcUntil(string rrule)
    {
        if (string.IsNullOrEmpty(rrule))
        {
            return true; // Will be caught by NotNull/MinimumLength rules
        }

        var untilMatch = UntilRegex().Match(rrule);
        if (!untilMatch.Success)
        {
            return true; // Will be caught by HaveUntilNotCount
        }

        var untilValue = untilMatch.Groups["until"].Value;
        return untilValue.EndsWith('Z');
    }

    /// <summary>
    /// Gets the error message when UNTIL is not in UTC format.
    /// </summary>
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

    [GeneratedRegex(@"(?:^|;)UNTIL\s*=\s*(?<until>[^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UntilRegex();
}
