namespace RecurringThings.Tests.Validation;

using System;
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
        // Act & Assert - no exception means success
        Validator.ValidateTypesFilter(null);
    }

    [Fact]
    public void ValidateTypesFilter_WhenPopulated_ShouldNotThrow()
    {
        // Arrange
        var types = new[] { "appointment", "meeting" };

        // Act & Assert - no exception means success
        Validator.ValidateTypesFilter(types);
    }

    [Fact]
    public void ValidateTypesFilter_WhenEmptyArray_ShouldThrowArgumentException()
    {
        // Arrange
        var types = Array.Empty<string>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Validator.ValidateTypesFilter(types));
        Assert.Contains("cannot be an empty array", ex.Message);
        Assert.Contains("Use null to include all types", ex.Message);
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

        // Act & Assert - no exception means success
        Validator.ValidateTenantScope(recurrence, "org1", "path1");
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
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Validator.ValidateTenantScope(recurrence, "org2", "path1"));
        Assert.Contains("Organization mismatch", ex.Message);
        Assert.Contains("must match parent recurrence", ex.Message);
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
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Validator.ValidateTenantScope(recurrence, "org1", "path2"));
        Assert.Contains("ResourcePath mismatch", ex.Message);
        Assert.Contains("must match parent recurrence", ex.Message);
    }

    [Fact]
    public void ValidateTenantScope_WhenParentRecurrenceNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            Validator.ValidateTenantScope(null!, "org", "path"));
    }

    #endregion
}
