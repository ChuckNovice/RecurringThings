namespace RecurringThings.Tests.Validation;

using System;
using System.Collections.Generic;
using FluentAssertions;
using RecurringThings.Domain;
using RecurringThings.Models;
using RecurringThings.Validation;
using Xunit;

public class ValidatorTests
{
    #region ValidateOrganization Tests

    [Fact]
    public void ValidateOrganization_WhenValid_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Validator.ValidateOrganization("valid-org");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrganization_WhenEmpty_ShouldNotThrow()
    {
        // Act & Assert - Empty is allowed
        var action = () => Validator.ValidateOrganization("");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrganization_WhenNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Validator.ValidateOrganization(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateOrganization_WhenExactlyAtMaxLength_ShouldNotThrow()
    {
        // Arrange
        var organization = new string('a', 100);

        // Act & Assert
        var action = () => Validator.ValidateOrganization(organization);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrganization_WhenExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var organization = new string('a', 101);

        // Act & Assert
        var action = () => Validator.ValidateOrganization(organization);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must not exceed 100 characters*");
    }

    #endregion

    #region ValidateResourcePath Tests

    [Fact]
    public void ValidateResourcePath_WhenValid_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Validator.ValidateResourcePath("user/calendar");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateResourcePath_WhenEmpty_ShouldNotThrow()
    {
        // Act & Assert - Empty is allowed
        var action = () => Validator.ValidateResourcePath("");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateResourcePath_WhenNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Validator.ValidateResourcePath(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateResourcePath_WhenExactlyAtMaxLength_ShouldNotThrow()
    {
        // Arrange
        var resourcePath = new string('a', 100);

        // Act & Assert
        var action = () => Validator.ValidateResourcePath(resourcePath);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateResourcePath_WhenExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var resourcePath = new string('a', 101);

        // Act & Assert
        var action = () => Validator.ValidateResourcePath(resourcePath);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must not exceed 100 characters*");
    }

    #endregion

    #region ValidateType Tests

    [Fact]
    public void ValidateType_WhenValid_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Validator.ValidateType("appointment");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateType_WhenSingleCharacter_ShouldNotThrow()
    {
        // Act & Assert - Minimum 1 character
        var action = () => Validator.ValidateType("a");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateType_WhenEmpty_ShouldThrowArgumentException()
    {
        // Act & Assert - Empty is NOT allowed
        var action = () => Validator.ValidateType("");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be at least 1 character*");
    }

    [Fact]
    public void ValidateType_WhenNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Validator.ValidateType(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateType_WhenExactlyAtMaxLength_ShouldNotThrow()
    {
        // Arrange
        var type = new string('a', 100);

        // Act & Assert
        var action = () => Validator.ValidateType(type);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateType_WhenExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var type = new string('a', 101);

        // Act & Assert
        var action = () => Validator.ValidateType(type);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must not exceed 100 characters*");
    }

    #endregion

    #region ValidateTimeZone Tests

    [Fact]
    public void ValidateTimeZone_WhenValidIanaTimeZone_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Validator.ValidateTimeZone("America/New_York");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateTimeZone_WhenUtc_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Validator.ValidateTimeZone("UTC");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateTimeZone_WhenEmpty_ShouldThrowArgumentException()
    {
        // Act & Assert - Empty is NOT allowed
        var action = () => Validator.ValidateTimeZone("");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be at least 1 character*");
    }

    [Fact]
    public void ValidateTimeZone_WhenNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Validator.ValidateTimeZone(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateTimeZone_WhenInvalidIanaTimeZone_ShouldThrowArgumentException()
    {
        // Act & Assert - Windows time zones are not valid IANA
        var action = () => Validator.ValidateTimeZone("Eastern Standard Time");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid IANA time zone*");
    }

    [Fact]
    public void ValidateTimeZone_WhenNonexistentTimeZone_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Validator.ValidateTimeZone("Invalid/TimeZone");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid IANA time zone*");
    }

    [Fact]
    public void ValidateTimeZone_WhenExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var timeZone = new string('a', 101);

        // Act & Assert
        var action = () => Validator.ValidateTimeZone(timeZone);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must not exceed 100 characters*");
    }

    #endregion

    #region ValidateRRule Tests

    [Fact]
    public void ValidateRRule_WhenValidWithUntil_ShouldNotThrow()
    {
        // Arrange
        var rrule = "FREQ=DAILY;UNTIL=20261231T235959Z";
        var endTime = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act & Assert
        var action = () => Validator.ValidateRRule(rrule, endTime);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateRRule_WhenEmpty_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Validator.ValidateRRule("", DateTime.UtcNow);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be at least 1 character*");
    }

    [Fact]
    public void ValidateRRule_WhenNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Validator.ValidateRRule(null!, DateTime.UtcNow);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateRRule_WhenExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var rrule = new string('a', 2001);

        // Act & Assert
        var action = () => Validator.ValidateRRule(rrule, DateTime.UtcNow);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must not exceed 2000 characters*");
    }

    [Fact]
    public void ValidateRRule_WhenContainsCount_ShouldThrowArgumentException()
    {
        // Arrange
        var rrule = "FREQ=DAILY;COUNT=10";

        // Act & Assert
        var action = () => Validator.ValidateRRule(rrule, DateTime.UtcNow);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*COUNT is not supported*");
    }

    [Fact]
    public void ValidateRRule_WhenContainsCountWithSpaces_ShouldThrowArgumentException()
    {
        // Arrange
        var rrule = "FREQ=DAILY;COUNT = 10";

        // Act & Assert
        var action = () => Validator.ValidateRRule(rrule, DateTime.UtcNow);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*COUNT is not supported*");
    }

    [Fact]
    public void ValidateRRule_WhenMissingUntil_ShouldThrowArgumentException()
    {
        // Arrange
        var rrule = "FREQ=DAILY;BYDAY=MO,TU,WE";

        // Act & Assert
        var action = () => Validator.ValidateRRule(rrule, DateTime.UtcNow);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must contain UNTIL*");
    }

    [Fact]
    public void ValidateRRule_WhenUntilNotUtc_ShouldThrowArgumentException()
    {
        // Arrange - Missing Z suffix
        var rrule = "FREQ=DAILY;UNTIL=20261231T235959";

        // Act & Assert
        var action = () => Validator.ValidateRRule(rrule, DateTime.UtcNow);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be in UTC*must end with 'Z'*");
    }

    [Fact]
    public void ValidateRRule_WhenUntilMismatchesEndTime_ShouldThrowArgumentException()
    {
        // Arrange
        var rrule = "FREQ=DAILY;UNTIL=20261231T235959Z";
        var endTime = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc); // Different year

        // Act & Assert
        var action = () => Validator.ValidateRRule(rrule, endTime);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must match RRule UNTIL*");
    }

    [Fact]
    public void ValidateRRule_WhenUntilWithinOneSeccondTolerance_ShouldNotThrow()
    {
        // Arrange
        var rrule = "FREQ=DAILY;UNTIL=20261231T235959Z";
        var endTime = new DateTime(2026, 12, 31, 23, 59, 58, DateTimeKind.Utc); // 1 second difference

        // Act & Assert
        var action = () => Validator.ValidateRRule(rrule, endTime);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateRRule_WhenUntilInvalidFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var rrule = "FREQ=DAILY;UNTIL=invalid-date-Z";

        // Act & Assert
        var action = () => Validator.ValidateRRule(rrule, DateTime.UtcNow);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid iCalendar date-time*");
    }

    [Fact]
    public void ValidateRRule_WhenComplexRuleWithUntil_ShouldNotThrow()
    {
        // Arrange
        var rrule = "FREQ=WEEKLY;BYDAY=MO,WE,FR;INTERVAL=2;UNTIL=20261231T235959Z";
        var endTime = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act & Assert
        var action = () => Validator.ValidateRRule(rrule, endTime);
        action.Should().NotThrow();
    }

    #endregion

    #region ValidateDuration Tests

    [Fact]
    public void ValidateDuration_WhenPositive_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Validator.ValidateDuration(TimeSpan.FromHours(1));
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateDuration_WhenOneSecond_ShouldNotThrow()
    {
        // Act & Assert - Minimum positive duration
        var action = () => Validator.ValidateDuration(TimeSpan.FromSeconds(1));
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateDuration_WhenZero_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Validator.ValidateDuration(TimeSpan.Zero);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be positive*");
    }

    [Fact]
    public void ValidateDuration_WhenNegative_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => Validator.ValidateDuration(TimeSpan.FromHours(-1));
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be positive*");
    }

    #endregion

    #region ValidateUtcDateTime Tests

    [Fact]
    public void ValidateUtcDateTime_WhenUtc_ShouldNotThrow()
    {
        // Arrange
        var dateTime = DateTime.UtcNow;

        // Act & Assert
        var action = () => Validator.ValidateUtcDateTime(dateTime, "testParam");
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateUtcDateTime_WhenLocal_ShouldThrowArgumentException()
    {
        // Arrange
        var dateTime = DateTime.Now;

        // Act & Assert
        var action = () => Validator.ValidateUtcDateTime(dateTime, "testParam");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be in UTC*Kind must be DateTimeKind.Utc*");
    }

    [Fact]
    public void ValidateUtcDateTime_WhenUnspecified_ShouldThrowArgumentException()
    {
        // Arrange
        var dateTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        // Act & Assert
        var action = () => Validator.ValidateUtcDateTime(dateTime, "testParam");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be in UTC*");
    }

    #endregion

    #region ValidateExtensions Tests

    [Fact]
    public void ValidateExtensions_WhenNull_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => Validator.ValidateExtensions(null);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateExtensions_WhenEmpty_ShouldNotThrow()
    {
        // Arrange
        var extensions = new Dictionary<string, string>();

        // Act & Assert
        var action = () => Validator.ValidateExtensions(extensions);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateExtensions_WhenValid_ShouldNotThrow()
    {
        // Arrange
        var extensions = new Dictionary<string, string>
        {
            ["color"] = "blue",
            ["priority"] = "high"
        };

        // Act & Assert
        var action = () => Validator.ValidateExtensions(extensions);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateExtensions_WhenKeyEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var extensions = new Dictionary<string, string>
        {
            [""] = "value"
        };

        // Act & Assert
        var action = () => Validator.ValidateExtensions(extensions);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be at least 1 character*");
    }

    [Fact]
    public void ValidateExtensions_WhenKeyExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var longKey = new string('a', 101);
        var extensions = new Dictionary<string, string>
        {
            [longKey] = "value"
        };

        // Act & Assert
        var action = () => Validator.ValidateExtensions(extensions);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must not exceed 100 characters*");
    }

    [Fact]
    public void ValidateExtensions_WhenKeyAtMaxLength_ShouldNotThrow()
    {
        // Arrange
        var maxKey = new string('a', 100);
        var extensions = new Dictionary<string, string>
        {
            [maxKey] = "value"
        };

        // Act & Assert
        var action = () => Validator.ValidateExtensions(extensions);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateExtensions_WhenValueExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var longValue = new string('a', 1025);
        var extensions = new Dictionary<string, string>
        {
            ["key"] = longValue
        };

        // Act & Assert
        var action = () => Validator.ValidateExtensions(extensions);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must not exceed 1024 characters*");
    }

    [Fact]
    public void ValidateExtensions_WhenValueAtMaxLength_ShouldNotThrow()
    {
        // Arrange
        var maxValue = new string('a', 1024);
        var extensions = new Dictionary<string, string>
        {
            ["key"] = maxValue
        };

        // Act & Assert
        var action = () => Validator.ValidateExtensions(extensions);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateExtensions_WhenValueEmpty_ShouldNotThrow()
    {
        // Arrange - Empty values are allowed
        var extensions = new Dictionary<string, string>
        {
            ["key"] = ""
        };

        // Act & Assert
        var action = () => Validator.ValidateExtensions(extensions);
        action.Should().NotThrow();
    }

    #endregion

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

    #region Validate RecurrenceCreate Tests

    [Fact]
    public void ValidateRecurrenceCreate_WhenValid_ShouldNotThrow()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "appointment",
            StartTimeUtc = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTimeUtc = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act & Assert
        var action = () => Validator.Validate(request);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateRecurrenceCreate_WhenNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Validator.Validate((RecurrenceCreate)null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateRecurrenceCreate_WhenInvalidType_ShouldThrow()
    {
        // Arrange
        var request = new RecurrenceCreate
        {
            Organization = "org",
            ResourcePath = "path",
            Type = "", // Invalid
            StartTimeUtc = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTimeUtc = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Act & Assert
        var action = () => Validator.Validate(request);
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Validate OccurrenceCreate Tests

    [Fact]
    public void ValidateOccurrenceCreate_WhenValid_ShouldNotThrow()
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

        // Act & Assert
        var action = () => Validator.Validate(request);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateOccurrenceCreate_WhenNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Validator.Validate((OccurrenceCreate)null!);
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Validate OccurrenceException Tests

    [Fact]
    public void ValidateOccurrenceException_WhenValid_ShouldNotThrow()
    {
        // Arrange
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Act & Assert
        var action = () => Validator.Validate(exception);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateOccurrenceException_WhenNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Validator.Validate((OccurrenceException)null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateOccurrenceException_WhenOriginalTimeNotUtc_ShouldThrow()
    {
        // Arrange
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.Now // Not UTC
        };

        // Act & Assert
        var action = () => Validator.Validate(exception);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be in UTC*");
    }

    #endregion

    #region Validate OccurrenceOverride Tests

    [Fact]
    public void ValidateOccurrenceOverride_WhenValid_ShouldNotThrow()
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
        @override.Initialize(DateTime.UtcNow, TimeSpan.FromHours(2));

        // Act & Assert
        var action = () => Validator.Validate(@override);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateOccurrenceOverride_WhenNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => Validator.Validate((OccurrenceOverride)null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateOccurrenceOverride_WhenDurationInvalid_ShouldThrow()
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
        @override.Initialize(DateTime.UtcNow, TimeSpan.Zero); // Invalid duration

        // Act & Assert
        var action = () => Validator.Validate(@override);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be positive*");
    }

    #endregion
}
