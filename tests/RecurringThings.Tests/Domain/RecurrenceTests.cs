namespace RecurringThings.Tests.Domain;

using System;
using System.Collections.Generic;
using FluentAssertions;
using RecurringThings.Domain;
using Xunit;

public class RecurrenceTests
{
    [Fact]
    public void Recurrence_WhenCreated_ShouldHaveAllRequiredProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromHours(1);
        var endTime = DateTime.UtcNow.AddYears(1);

        // Act
        var recurrence = new Recurrence
        {
            Id = id,
            Organization = "org1",
            ResourcePath = "user/calendar",
            Type = "appointment",
            StartTime = startTime,
            Duration = duration,
            RecurrenceEndTime = endTime,
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "America/New_York"
        };

        // Assert
        recurrence.Id.Should().Be(id);
        recurrence.Organization.Should().Be("org1");
        recurrence.ResourcePath.Should().Be("user/calendar");
        recurrence.Type.Should().Be("appointment");
        recurrence.StartTime.Should().Be(startTime);
        recurrence.Duration.Should().Be(duration);
        recurrence.RecurrenceEndTime.Should().Be(endTime);
        recurrence.RRule.Should().Be("FREQ=DAILY;UNTIL=20261231T235959Z");
        recurrence.TimeZone.Should().Be("America/New_York");
    }

    [Fact]
    public void Recurrence_WhenCreatedWithEmptyOrganization_ShouldBeValid()
    {
        // Arrange & Act
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = "",
            ResourcePath = "path",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC"
        };

        // Assert
        recurrence.Organization.Should().BeEmpty();
    }

    [Fact]
    public void Recurrence_WhenCreatedWithEmptyResourcePath_ShouldBeValid()
    {
        // Arrange & Act
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC"
        };

        // Assert
        recurrence.ResourcePath.Should().BeEmpty();
    }

    [Fact]
    public void Recurrence_Duration_ShouldBeMutable()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC"
        };

        // Act
        recurrence.Duration = TimeSpan.FromHours(2);

        // Assert
        recurrence.Duration.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Recurrence_Extensions_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC"
        };

        // Assert
        recurrence.Extensions.Should().BeNull();
    }

    [Fact]
    public void Recurrence_Extensions_ShouldBeMutable()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC",
            Extensions = new Dictionary<string, string> { ["key1"] = "value1" }
        };

        // Act
        recurrence.Extensions = new Dictionary<string, string>
        {
            ["key2"] = "value2",
            ["key3"] = "value3"
        };

        // Assert
        recurrence.Extensions.Should().HaveCount(2);
        recurrence.Extensions.Should().ContainKey("key2");
        recurrence.Extensions.Should().ContainKey("key3");
    }

    [Fact]
    public void Recurrence_Extensions_CanBeSetToNull()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC",
            Extensions = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Act
        recurrence.Extensions = null;

        // Assert
        recurrence.Extensions.Should().BeNull();
    }

    [Fact]
    public void Recurrence_WhenCreatedWithExtensions_ShouldContainAllPairs()
    {
        // Arrange
        var extensions = new Dictionary<string, string>
        {
            ["color"] = "blue",
            ["priority"] = "high",
            ["description"] = "Important meeting"
        };

        // Act
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC",
            Extensions = extensions
        };

        // Assert
        recurrence.Extensions.Should().BeEquivalentTo(extensions);
    }

    [Fact]
    public void Recurrence_Id_ShouldBeInitOnly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var recurrence = new Recurrence
        {
            Id = id,
            Organization = "org",
            ResourcePath = "path",
            Type = "type",
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = DateTime.UtcNow.AddYears(1),
            RRule = "FREQ=DAILY;UNTIL=20261231T235959Z",
            TimeZone = "UTC"
        };

        // Assert - Id is init-only, so we just verify it was set correctly
        recurrence.Id.Should().Be(id);
    }
}
