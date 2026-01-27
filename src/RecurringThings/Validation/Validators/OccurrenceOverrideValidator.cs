namespace RecurringThings.Validation.Validators;

using FluentValidation;
using RecurringThings.Domain;

/// <summary>
/// FluentValidation validator for <see cref="OccurrenceOverride"/> entities.
/// </summary>
internal sealed class OccurrenceOverrideValidator : AbstractValidator<OccurrenceOverride>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OccurrenceOverrideValidator"/> class.
    /// </summary>
    public OccurrenceOverrideValidator()
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

        RuleFor(x => x.OriginalTimeUtc)
            .MustBeUtc();

        RuleFor(x => x.StartTime)
            .MustBeUtc();

        RuleFor(x => x.Duration)
            .MustBePositive();

        RuleFor(x => x.Extensions)
            .ValidExtensions();

        RuleFor(x => x.OriginalExtensions)
            .ValidExtensions();
    }
}
