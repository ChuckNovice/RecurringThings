namespace RecurringThings.Validation.Validators;

using FluentValidation;
using RecurringThings.Domain;

/// <summary>
/// FluentValidation validator for <see cref="OccurrenceException"/> entities.
/// </summary>
internal sealed class OccurrenceExceptionValidator : AbstractValidator<OccurrenceException>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OccurrenceExceptionValidator"/> class.
    /// </summary>
    public OccurrenceExceptionValidator()
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

        RuleFor(x => x.Extensions)
            .ValidExtensions();
    }
}
