namespace RecurringThings.Tests.Validation;

using System;
using FluentAssertions;
using RecurringThings.Domain;
using RecurringThings.Validation;
using Xunit;

/// <summary>
/// Tests for the <see cref="Validator"/> class.
/// </summary>
/// <remarks>
/// Most validation logic has been migrated to FluentValidation validators.
/// See validator-specific test classes for entity validation tests.
/// </remarks>
public class ValidatorTests
{
    #region ValidateTypesFilter Tests

    [Fact]
    public void ValidateTypesFilter_WhenNull_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Validator.ValidateTypesFilter(null);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateTypesFilter_WhenPopulated_ShouldNotThrow()
    {
        // Arrange
        var types = new[] { "appointment", "meeting" };

        // Act & Assert
        var action = () => Validator.ValidateTypesFilter(types);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateTypesFilter_WhenEmptyArray_ShouldThrowArgumentException()
    {
        // Arrange
        var types = Array.Empty<string>();

        // Act & Assert
        var action = () => Validator.ValidateTypesFilter(types);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be an empty array*Use null to include all types*");
    }

    #endregion

    #region ValidateTenantScope Tests

    [Fact]
    public void ValidateTenantScope_WhenMatching_ShouldNotThrow()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org1",
            ResourcePath = "path1",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC"
        };

        // Act & Assert
        var action = () => Validator.ValidateTenantScope(recurrence, "org1", "path1");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateTenantScope_WhenOrganizationMismatch_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org1",
            ResourcePath = "path1",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC"
        };

        // Act & Assert
        var action = () => Validator.ValidateTenantScope(recurrence, "org2", "path1");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Organization mismatch*must match parent recurrence*");
    }

    [Fact]
    public void ValidateTenantScope_WhenResourcePathMismatch_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org1",
            ResourcePath = "path1",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC"
        };

        // Act & Assert
        var action = () => Validator.ValidateTenantScope(recurrence, "org1", "path2");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*ResourcePath mismatch*must match parent recurrence*");
    }

    [Fact]
    public void ValidateTenantScope_WhenParentRecurrenceNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Validator.ValidateTenantScope(null!, "org", "path");
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion
}
