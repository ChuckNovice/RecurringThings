namespace RecurringThings.Tests.Validation.Validators;

using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.TestHelper;
using RecurringThings.Models;
using RecurringThings.Validation.Validators;
using Xunit;

/// <summary>
/// Tests for the <see cref="RecurrenceCreateValidator"/> class.
/// </summary>
public class RecurrenceCreateValidatorTests
{
    private readonly RecurrenceCreateValidator _validator = new();

    #region Valid Request Tests

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenValidWithExtensions_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York",
            Extensions = new Dictionary<string, string>
            {
                ["color"] = "blue",
                ["priority"] = "high"
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Organization Tests

    [Fact]
    public void Validate_WhenOrganizationNull_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = null!,
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Organization);
    }

    [Fact]
    public void Validate_WhenOrganizationEmpty_ShouldNotHaveError()
    {
        // Arrange - Empty is allowed
        var request = new RecurrenceCreate
        {
            Organization = "",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Organization);
    }

    [Fact]
    public void Validate_WhenOrganizationExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = new string('a', 101),
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Organization);
        result.Errors.Should().Contain(e => e.PropertyName == "Organization" && e.ErrorMessage.Contains("must not exceed 100 characters"));
    }

    #endregion

    #region Type Tests

    [Fact]
    public void Validate_WhenTypeNull_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = null!,
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_WhenTypeEmpty_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    #endregion

    #region TimeZone Tests

    [Fact]
    public void Validate_WhenTimeZoneInvalidIana_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "Eastern Standard Time" // Windows time zone, not IANA
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TimeZone);
        result.Errors.Should().Contain(e => e.PropertyName == "TimeZone" && e.ErrorMessage.Contains("not a valid IANA time zone"));
    }

    #endregion

    #region RRule Tests

    [Fact]
    public void Validate_WhenRRuleContainsCount_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;COUNT=10",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RRule);
        result.Errors.Should().Contain(e => e.PropertyName == "RRule" && e.ErrorMessage.Contains("COUNT is not supported"));
    }

    [Fact]
    public void Validate_WhenRRuleMissingUntil_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;BYDAY=MO,TU,WE",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RRule);
        result.Errors.Should().Contain(e => e.PropertyName == "RRule" && e.ErrorMessage.Contains("must contain UNTIL"));
    }

    [Fact]
    public void Validate_WhenRRuleUntilNotUtc_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959", // Missing Z
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RRule);
    }

    #endregion

    #region StartTime Tests

    [Fact]
    public void Validate_WhenStartTimeUtc_ShouldNotHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
    }

    [Fact]
    public void Validate_WhenStartTimeLocal_ShouldNotHaveError()
    {
        // Arrange - Local time is now accepted and converted to UTC internally
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.Now, // Local time
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
    }

    [Fact]
    public void Validate_WhenStartTimeUnspecified_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = new DateTime(2025, 6, 1, 9, 0, 0, DateTimeKind.Unspecified),
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StartTime);
        result.Errors.Should().Contain(e => e.PropertyName == "StartTime" && e.ErrorMessage.Contains("Unspecified"));
    }

    #endregion

    #region Duration Tests

    [Fact]
    public void Validate_WhenDurationZero_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.Zero,
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Duration);
        result.Errors.Should().Contain(e => e.PropertyName == "Duration" && e.ErrorMessage.Contains("must be positive"));
    }

    #endregion

    #region Extensions Tests

    [Fact]
    public void Validate_WhenExtensionKeyEmpty_ShouldHaveError()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York",
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

    #endregion
}
