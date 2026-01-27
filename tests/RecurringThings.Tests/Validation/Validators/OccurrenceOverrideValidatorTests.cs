namespace RecurringThings.Tests.Validation.Validators;

using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.TestHelper;
using RecurringThings.Domain;
using RecurringThings.Validation.Validators;
using Xunit;

/// <summary>
/// Tests for the <see cref="OccurrenceOverrideValidator"/> class.
/// </summary>
public class OccurrenceOverrideValidatorTests
{
    private readonly OccurrenceOverrideValidator _validator = new();

    private static OccurrenceOverride CreateValidOverride()
    {
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));
        return @override;
    }

    #region Valid Entity Tests

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        // Arrange
        var @override = CreateValidOverride();

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenValidWithExtensions_ShouldNotHaveErrors()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1),
            Extensions = new Dictionary<string, string>
            {
                ["location"] = "New Conference Room"
            },
            OriginalExtensions = new Dictionary<string, string>
            {
                ["location"] = "Original Conference Room"
            }
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Organization Tests

    [Fact]
    public void Validate_WhenOrganizationNull_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = null!,
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Organization);
    }

    [Fact]
    public void Validate_WhenOrganizationEmpty_ShouldNotHaveError()
    {
        // Arrange - Empty is allowed
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Organization);
    }

    [Fact]
    public void Validate_WhenOrganizationExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = new string('a', 101),
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Organization);
        result.Errors.Should().Contain(e => e.PropertyName == "Organization" && e.ErrorMessage.Contains("must not exceed 100 characters"));
    }

    #endregion

    #region ResourcePath Tests

    [Fact]
    public void Validate_WhenResourcePathNull_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = null!,
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResourcePath);
    }

    [Fact]
    public void Validate_WhenResourcePathEmpty_ShouldNotHaveError()
    {
        // Arrange - Empty is allowed
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ResourcePath);
    }

    [Fact]
    public void Validate_WhenResourcePathExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = new string('a', 101),
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResourcePath);
        result.Errors.Should().Contain(e => e.PropertyName == "ResourcePath" && e.ErrorMessage.Contains("must not exceed 100 characters"));
    }

    #endregion

    #region OriginalTimeUtc Tests

    [Fact]
    public void Validate_WhenOriginalTimeUtcNotUtc_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.Now, // Local time
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalTimeUtc);
        result.Errors.Should().Contain(e => e.PropertyName == "OriginalTimeUtc" && e.ErrorMessage.Contains("must be in UTC"));
    }

    [Fact]
    public void Validate_WhenOriginalTimeUtcIsUtc_ShouldNotHaveError()
    {
        // Arrange
        var @override = CreateValidOverride();

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OriginalTimeUtc);
    }

    #endregion

    #region StartTime Tests

    [Fact]
    public void Validate_WhenStartTimeNotUtc_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.Now, TimeSpan.FromHours(2)); // Local time

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StartTime);
        result.Errors.Should().Contain(e => e.PropertyName == "StartTime" && e.ErrorMessage.Contains("must be in UTC"));
    }

    [Fact]
    public void Validate_WhenStartTimeIsUtc_ShouldNotHaveError()
    {
        // Arrange
        var @override = CreateValidOverride();

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
    }

    #endregion

    #region Duration Tests

    [Fact]
    public void Validate_WhenDurationZero_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow, TimeSpan.Zero);

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Duration);
        result.Errors.Should().Contain(e => e.PropertyName == "Duration" && e.ErrorMessage.Contains("must be positive"));
    }

    [Fact]
    public void Validate_WhenDurationNegative_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow, TimeSpan.FromHours(-1));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Duration);
        result.Errors.Should().Contain(e => e.PropertyName == "Duration" && e.ErrorMessage.Contains("must be positive"));
    }

    [Fact]
    public void Validate_WhenDurationPositive_ShouldNotHaveError()
    {
        // Arrange
        var @override = CreateValidOverride();

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Duration);
    }

    #endregion

    #region Extensions Tests

    [Fact]
    public void Validate_WhenExtensionsNull_ShouldNotHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1),
            Extensions = null
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Extensions);
    }

    [Fact]
    public void Validate_WhenExtensionKeyEmpty_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1),
            Extensions = new Dictionary<string, string>
            {
                [""] = "value"
            }
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Extensions);
        result.Errors.Should().Contain(e => e.PropertyName == "Extensions" && e.ErrorMessage.Contains("must be at least 1 character"));
    }

    [Fact]
    public void Validate_WhenExtensionKeyExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1),
            Extensions = new Dictionary<string, string>
            {
                [new string('a', 101)] = "value"
            }
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Extensions);
        result.Errors.Should().Contain(e => e.PropertyName == "Extensions" && e.ErrorMessage.Contains("must not exceed 100 characters"));
    }

    [Fact]
    public void Validate_WhenExtensionValueExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1),
            Extensions = new Dictionary<string, string>
            {
                ["key"] = new string('a', 1025)
            }
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Extensions);
        result.Errors.Should().Contain(e => e.PropertyName == "Extensions" && e.ErrorMessage.Contains("must not exceed 1024 characters"));
    }

    #endregion

    #region OriginalExtensions Tests

    [Fact]
    public void Validate_WhenOriginalExtensionsNull_ShouldNotHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1),
            OriginalExtensions = null
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OriginalExtensions);
    }

    [Fact]
    public void Validate_WhenOriginalExtensionKeyEmpty_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1),
            OriginalExtensions = new Dictionary<string, string>
            {
                [""] = "value"
            }
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalExtensions);
        result.Errors.Should().Contain(e => e.PropertyName == "OriginalExtensions" && e.ErrorMessage.Contains("must be at least 1 character"));
    }

    [Fact]
    public void Validate_WhenOriginalExtensionKeyExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1),
            OriginalExtensions = new Dictionary<string, string>
            {
                [new string('a', 101)] = "value"
            }
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalExtensions);
        result.Errors.Should().Contain(e => e.PropertyName == "OriginalExtensions" && e.ErrorMessage.Contains("must not exceed 100 characters"));
    }

    [Fact]
    public void Validate_WhenOriginalExtensionValueExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1),
            OriginalExtensions = new Dictionary<string, string>
            {
                ["key"] = new string('a', 1025)
            }
        };
        @override.Initialize(DateTime.UtcNow.AddHours(1), TimeSpan.FromHours(2));

        // Act
        var result = _validator.TestValidate(@override);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalExtensions);
        result.Errors.Should().Contain(e => e.PropertyName == "OriginalExtensions" && e.ErrorMessage.Contains("must not exceed 1024 characters"));
    }

    #endregion
}
