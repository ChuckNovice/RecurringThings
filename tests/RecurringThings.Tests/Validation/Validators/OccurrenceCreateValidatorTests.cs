namespace RecurringThings.Tests.Validation.Validators;

using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.TestHelper;
using RecurringThings.Models;
using RecurringThings.Validation.Validators;
using Xunit;

/// <summary>
/// Tests for the <see cref="OccurrenceCreateValidator"/> class.
/// </summary>
public class OccurrenceCreateValidatorTests
{
    private readonly OccurrenceCreateValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new OccurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            StartTimeUtc = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            TimeZone = "Europe/London"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOrganizationNull_ShouldHaveError()
    {
        // Arrange
        var request = new OccurrenceCreate
        {
            Organization = null!,
            ResourcePath = "path",
            Type = "meeting",
            StartTimeUtc = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            TimeZone = "Europe/London"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Organization);
    }

    [Fact]
    public void Validate_WhenTypeEmpty_ShouldHaveError()
    {
        // Arrange
        var request = new OccurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "",
            StartTimeUtc = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            TimeZone = "Europe/London"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_WhenTimeZoneInvalid_ShouldHaveError()
    {
        // Arrange
        var request = new OccurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            StartTimeUtc = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            TimeZone = "Invalid/TimeZone"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TimeZone);
        result.Errors.Should().Contain(e => e.PropertyName == "TimeZone" && e.ErrorMessage.Contains("not a valid IANA time zone"));
    }

    [Fact]
    public void Validate_WhenStartTimeUtcNotUtc_ShouldHaveError()
    {
        // Arrange
        var request = new OccurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            StartTimeUtc = DateTime.Now,
            Duration = TimeSpan.FromHours(1),
            TimeZone = "Europe/London"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StartTimeUtc);
        result.Errors.Should().Contain(e => e.PropertyName == "StartTimeUtc" && e.ErrorMessage.Contains("must be in UTC"));
    }

    [Fact]
    public void Validate_WhenDurationZero_ShouldHaveError()
    {
        // Arrange
        var request = new OccurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            StartTimeUtc = DateTime.UtcNow,
            Duration = TimeSpan.Zero,
            TimeZone = "Europe/London"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Duration);
        result.Errors.Should().Contain(e => e.PropertyName == "Duration" && e.ErrorMessage.Contains("must be positive"));
    }

    [Fact]
    public void Validate_WhenExtensionKeyEmpty_ShouldHaveError()
    {
        // Arrange
        var request = new OccurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            StartTimeUtc = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            TimeZone = "Europe/London",
            Extensions = new Dictionary<string, string>
            {
                [""] = "value"
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Extensions);
    }
}
