namespace RecurringThings.Validation.Validators;

using FluentValidation;
using RecurringThings.Models;

/// <summary>
/// FluentValidation validator for <see cref="OccurrenceCreate"/> requests.
/// </summary>
internal sealed class OccurrenceCreateValidator : AbstractValidator<OccurrenceCreate>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OccurrenceCreateValidator"/> class.
    /// </summary>
    public OccurrenceCreateValidator()
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

        RuleFor(x => x.StartTime)
            .MustNotBeUnspecified();

        RuleFor(x => x.Duration)
            .MustBePositive();

        RuleFor(x => x.Extensions)
            .ValidExtensions();
    }
}
