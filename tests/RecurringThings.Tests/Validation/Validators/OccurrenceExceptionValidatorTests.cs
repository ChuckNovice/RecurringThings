namespace RecurringThings.Tests.Validation.Validators;

using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.TestHelper;
using RecurringThings.Domain;
using RecurringThings.Validation.Validators;
using Xunit;

/// <summary>
/// Tests for the <see cref="OccurrenceExceptionValidator"/> class.
/// </summary>
public class OccurrenceExceptionValidatorTests
{
    private readonly OccurrenceExceptionValidator _validator = new();

    private static OccurrenceException CreateValidException() => new()
    {
        Id = Guid.NewGuid(),
        Organization = "org",
        ResourcePath = "path",
        RecurrenceId = Guid.NewGuid(),
        OriginalTimeUtc = DateTime.UtcNow
    };

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        // Arrange
        var exception = CreateValidException();

        // Act
        var result = _validator.TestValidate(exception);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOrganizationNull_ShouldHaveError()
    {
        // Arrange
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = null!,
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Act
        var result = _validator.TestValidate(exception);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Organization);
    }

    [Fact]
    public void Validate_WhenOrganizationExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = new string('a', 101),
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Act
        var result = _validator.TestValidate(exception);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Organization);
        result.Errors.Should().Contain(e => e.PropertyName == "Organization" && e.ErrorMessage.Contains("must not exceed 100 characters"));
    }

    [Fact]
    public void Validate_WhenOriginalTimeUtcNotUtc_ShouldHaveError()
    {
        // Arrange
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.Now // Local time
        };

        // Act
        var result = _validator.TestValidate(exception);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalTimeUtc);
        result.Errors.Should().Contain(e => e.PropertyName == "OriginalTimeUtc" && e.ErrorMessage.Contains("must be in UTC"));
    }

    [Fact]
    public void Validate_WhenExtensionKeyEmpty_ShouldHaveError()
    {
        // Arrange
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            Extensions = new Dictionary<string, string>
            {
                [""] = "value"
            }
        };

        // Act
        var result = _validator.TestValidate(exception);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Extensions);
    }
}
