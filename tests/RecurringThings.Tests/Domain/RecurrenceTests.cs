namespace RecurringThings.Tests.Domain;

using System;
using System.Collections.Generic;
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
        Assert.Equal(id, recurrence.Id);
        Assert.Equal("org1", recurrence.Organization);
        Assert.Equal("user/calendar", recurrence.ResourcePath);
        Assert.Equal("appointment", recurrence.Type);
        Assert.Equal(startTime, recurrence.StartTime);
        Assert.Equal(duration, recurrence.Duration);
        Assert.Equal(endTime, recurrence.RecurrenceEndTime);
        Assert.Equal("FREQ=DAILY;UNTIL=20261231T235959Z", recurrence.RRule);
        Assert.Equal("America/New_York", recurrence.TimeZone);
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
        Assert.Empty(recurrence.Organization);
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
        Assert.Empty(recurrence.ResourcePath);
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
        Assert.Equal(TimeSpan.FromHours(2), recurrence.Duration);
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
        Assert.Null(recurrence.Extensions);
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
        Assert.Equal(2, recurrence.Extensions.Count);
        Assert.True(recurrence.Extensions.ContainsKey("key2"));
        Assert.True(recurrence.Extensions.ContainsKey("key3"));
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
        Assert.Null(recurrence.Extensions);
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
        Assert.Equivalent(extensions, recurrence.Extensions);
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
        Assert.Equal(id, recurrence.Id);
    }
}
