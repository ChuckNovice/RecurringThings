namespace RecurringThings.Tests.Domain;

using System;
using System.Collections.Generic;
using RecurringThings.Domain;
using Xunit;

public class OccurrenceTests
{
    [Fact]
    public void Occurrence_WhenInitialized_ShouldComputeEndTime()
    {
        // Arrange
        var startTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(2);

        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            TimeZone = "America/New_York"
        };

        // Act
        occurrence.Initialize(startTime, duration);

        // Assert
        Assert.Equal(startTime, occurrence.StartTime);
        Assert.Equal(duration, occurrence.Duration);
        Assert.Equal(startTime.Add(duration), occurrence.EndTime);
    }

    [Fact]
    public void Occurrence_WhenStartTimeChanges_ShouldRecomputeEndTime()
    {
        // Arrange
        var initialStartTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(2);

        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            TimeZone = "America/New_York"
        };
        occurrence.Initialize(initialStartTime, duration);

        // Act
        var newStartTime = new DateTime(2026, 1, 15, 14, 0, 0, DateTimeKind.Utc);
        occurrence.StartTime = newStartTime;

        // Assert
        Assert.Equal(newStartTime, occurrence.StartTime);
        Assert.Equal(newStartTime.Add(duration), occurrence.EndTime);
    }

    [Fact]
    public void Occurrence_WhenDurationChanges_ShouldRecomputeEndTime()
    {
        // Arrange
        var startTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var initialDuration = TimeSpan.FromHours(1);

        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            TimeZone = "America/New_York"
        };
        occurrence.Initialize(startTime, initialDuration);

        // Act
        var newDuration = TimeSpan.FromHours(3);
        occurrence.Duration = newDuration;

        // Assert
        Assert.Equal(newDuration, occurrence.Duration);
        Assert.Equal(startTime.Add(newDuration), occurrence.EndTime);
    }

    [Fact]
    public void Occurrence_WhenBothStartTimeAndDurationChange_ShouldRecomputeEndTimeCorrectly()
    {
        // Arrange
        var initialStartTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var initialDuration = TimeSpan.FromHours(1);

        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            TimeZone = "America/New_York"
        };
        occurrence.Initialize(initialStartTime, initialDuration);

        // Act
        var newStartTime = new DateTime(2026, 1, 16, 9, 0, 0, DateTimeKind.Utc);
        var newDuration = TimeSpan.FromMinutes(90);
        occurrence.StartTime = newStartTime;
        occurrence.Duration = newDuration;

        // Assert
        Assert.Equal(newStartTime, occurrence.StartTime);
        Assert.Equal(newDuration, occurrence.Duration);
        Assert.Equal(newStartTime.Add(newDuration), occurrence.EndTime);
    }

    [Fact]
    public void Occurrence_WithZeroDuration_ShouldHaveEqualStartAndEndTime()
    {
        // Arrange
        var startTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.Zero;

        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "instant-event",
            TimeZone = "UTC"
        };

        // Act
        occurrence.Initialize(startTime, duration);

        // Assert
        Assert.Equal(occurrence.StartTime, occurrence.EndTime);
    }

    [Fact]
    public void Occurrence_Extensions_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            TimeZone = "UTC"
        };
        occurrence.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Assert
        Assert.Null(occurrence.Extensions);
    }

    [Fact]
    public void Occurrence_Extensions_ShouldBeMutable()
    {
        // Arrange
        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            TimeZone = "UTC",
            Extensions = new Dictionary<string, string> { ["key1"] = "value1" }
        };
        occurrence.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Act
        occurrence.Extensions = new Dictionary<string, string>
        {
            ["key2"] = "value2"
        };

        // Assert
        Assert.Single(occurrence.Extensions);
        Assert.True(occurrence.Extensions.ContainsKey("key2"));
        Assert.False(occurrence.Extensions.ContainsKey("key1"));
    }

    [Fact]
    public void Occurrence_WhenCreatedWithAllProperties_ShouldHaveCorrectValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var startTime = new DateTime(2026, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(45);
        var extensions = new Dictionary<string, string>
        {
            ["location"] = "Conference Room A",
            ["attendees"] = "5"
        };

        // Act
        var occurrence = new Occurrence
        {
            Id = id,
            Organization = "acme-corp",
            ResourcePath = "team/engineering",
            Type = "standup",
            TimeZone = "Europe/London",
            Extensions = extensions
        };
        occurrence.Initialize(startTime, duration);

        // Assert
        Assert.Equal(id, occurrence.Id);
        Assert.Equal("acme-corp", occurrence.Organization);
        Assert.Equal("team/engineering", occurrence.ResourcePath);
        Assert.Equal("standup", occurrence.Type);
        Assert.Equal("Europe/London", occurrence.TimeZone);
        Assert.Equal(startTime, occurrence.StartTime);
        Assert.Equal(duration, occurrence.Duration);
        Assert.Equal(new DateTime(2026, 6, 15, 15, 15, 0, DateTimeKind.Utc), occurrence.EndTime);
        Assert.Equivalent(extensions, occurrence.Extensions);
    }

    [Fact]
    public void Occurrence_WhenCreatedWithEmptyOrganization_ShouldBeValid()
    {
        // Arrange & Act
        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = "",
            ResourcePath = "path",
            Type = "type",
            TimeZone = "UTC"
        };
        occurrence.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Assert
        Assert.Empty(occurrence.Organization);
    }

    [Fact]
    public void Occurrence_WhenCreatedWithEmptyResourcePath_ShouldBeValid()
    {
        // Arrange & Act
        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "",
            Type = "type",
            TimeZone = "UTC"
        };
        occurrence.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Assert
        Assert.Empty(occurrence.ResourcePath);
    }

    [Fact]
    public void Occurrence_Initialize_ShouldOverridePreviousValues()
    {
        // Arrange
        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "type",
            TimeZone = "UTC"
        };
        occurrence.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Act - Re-initialize with different values
        var newStartTime = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDuration = TimeSpan.FromMinutes(30);
        occurrence.Initialize(newStartTime, newDuration);

        // Assert
        Assert.Equal(newStartTime, occurrence.StartTime);
        Assert.Equal(newDuration, occurrence.Duration);
        Assert.Equal(newStartTime.Add(newDuration), occurrence.EndTime);
    }
}
