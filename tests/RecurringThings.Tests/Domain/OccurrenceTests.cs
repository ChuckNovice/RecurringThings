namespace RecurringThings.Tests.Domain;

using System;
using System.Collections.Generic;
using FluentAssertions;
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
        occurrence.StartTime.Should().Be(startTime);
        occurrence.Duration.Should().Be(duration);
        occurrence.EndTime.Should().Be(startTime.Add(duration));
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
        occurrence.StartTime.Should().Be(newStartTime);
        occurrence.EndTime.Should().Be(newStartTime.Add(duration));
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
        occurrence.Duration.Should().Be(newDuration);
        occurrence.EndTime.Should().Be(startTime.Add(newDuration));
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
        occurrence.StartTime.Should().Be(newStartTime);
        occurrence.Duration.Should().Be(newDuration);
        occurrence.EndTime.Should().Be(newStartTime.Add(newDuration));
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
        occurrence.EndTime.Should().Be(occurrence.StartTime);
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
        occurrence.Extensions.Should().BeNull();
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
        occurrence.Extensions.Should().HaveCount(1);
        occurrence.Extensions.Should().ContainKey("key2");
        occurrence.Extensions.Should().NotContainKey("key1");
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
        occurrence.Id.Should().Be(id);
        occurrence.Organization.Should().Be("acme-corp");
        occurrence.ResourcePath.Should().Be("team/engineering");
        occurrence.Type.Should().Be("standup");
        occurrence.TimeZone.Should().Be("Europe/London");
        occurrence.StartTime.Should().Be(startTime);
        occurrence.Duration.Should().Be(duration);
        occurrence.EndTime.Should().Be(new DateTime(2026, 6, 15, 15, 15, 0, DateTimeKind.Utc));
        occurrence.Extensions.Should().BeEquivalentTo(extensions);
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
        occurrence.Organization.Should().BeEmpty();
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
        occurrence.ResourcePath.Should().BeEmpty();
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
        occurrence.StartTime.Should().Be(newStartTime);
        occurrence.Duration.Should().Be(newDuration);
        occurrence.EndTime.Should().Be(newStartTime.Add(newDuration));
    }
}
