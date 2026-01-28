namespace RecurringThings.Tests.Domain;

using System;
using System.Collections.Generic;
using RecurringThings.Domain;
using Xunit;

public class OccurrenceOverrideTests
{
    [Fact]
    public void OccurrenceOverride_WhenInitialized_ShouldComputeEndTime()
    {
        // Arrange
        var startTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(2);

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

        // Act
        @override.Initialize(startTime, duration);

        // Assert
        Assert.Equal(startTime, @override.StartTime);
        Assert.Equal(duration, @override.Duration);
        Assert.Equal(startTime.Add(duration), @override.EndTime);
    }

    [Fact]
    public void OccurrenceOverride_WhenStartTimeChanges_ShouldRecomputeEndTime()
    {
        // Arrange
        var initialStartTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(2);

        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(initialStartTime, duration);

        // Act
        var newStartTime = new DateTime(2026, 1, 15, 14, 0, 0, DateTimeKind.Utc);
        @override.StartTime = newStartTime;

        // Assert
        Assert.Equal(newStartTime, @override.StartTime);
        Assert.Equal(newStartTime.Add(duration), @override.EndTime);
    }

    [Fact]
    public void OccurrenceOverride_WhenDurationChanges_ShouldRecomputeEndTime()
    {
        // Arrange
        var startTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var initialDuration = TimeSpan.FromHours(1);

        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(startTime, initialDuration);

        // Act
        var newDuration = TimeSpan.FromHours(3);
        @override.Duration = newDuration;

        // Assert
        Assert.Equal(newDuration, @override.Duration);
        Assert.Equal(startTime.Add(newDuration), @override.EndTime);
    }

    [Fact]
    public void OccurrenceOverride_WhenBothStartTimeAndDurationChange_ShouldRecomputeEndTimeCorrectly()
    {
        // Arrange
        var initialStartTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var initialDuration = TimeSpan.FromHours(1);

        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(initialStartTime, initialDuration);

        // Act
        var newStartTime = new DateTime(2026, 1, 16, 9, 0, 0, DateTimeKind.Utc);
        var newDuration = TimeSpan.FromMinutes(90);
        @override.StartTime = newStartTime;
        @override.Duration = newDuration;

        // Assert
        Assert.Equal(newStartTime, @override.StartTime);
        Assert.Equal(newDuration, @override.Duration);
        Assert.Equal(newStartTime.Add(newDuration), @override.EndTime);
    }

    [Fact]
    public void OccurrenceOverride_OriginalDuration_ShouldBeDenormalized()
    {
        // Arrange
        var originalDuration = TimeSpan.FromHours(1);

        // Act
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = originalDuration
        };
        @override.Initialize(DateTime.UtcNow, TimeSpan.FromHours(2));

        // Assert - OriginalDuration should remain unchanged
        Assert.Equal(originalDuration, @override.OriginalDuration);
        Assert.NotEqual(originalDuration, @override.Duration);
    }

    [Fact]
    public void OccurrenceOverride_OriginalExtensions_ShouldBeDenormalized()
    {
        // Arrange
        var originalExtensions = new Dictionary<string, string>
        {
            ["color"] = "blue",
            ["priority"] = "high"
        };

        // Act
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1),
            OriginalExtensions = originalExtensions,
            Extensions = new Dictionary<string, string> { ["color"] = "red" }
        };
        @override.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Assert - OriginalExtensions should remain unchanged
        Assert.Equivalent(originalExtensions, @override.OriginalExtensions);
        Assert.NotEqual(originalExtensions, @override.Extensions);
    }

    [Fact]
    public void OccurrenceOverride_Extensions_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Assert
        Assert.Null(@override.Extensions);
    }

    [Fact]
    public void OccurrenceOverride_Extensions_ShouldBeMutable()
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
            Extensions = new Dictionary<string, string> { ["key1"] = "value1" }
        };
        @override.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Act
        @override.Extensions = new Dictionary<string, string>
        {
            ["key2"] = "value2"
        };

        // Assert
        Assert.Single(@override.Extensions);
        Assert.True(@override.Extensions.ContainsKey("key2"));
        Assert.False(@override.Extensions.ContainsKey("key1"));
    }

    [Fact]
    public void OccurrenceOverride_OriginalExtensions_CanBeNull()
    {
        // Arrange & Act
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

        // Assert
        Assert.Null(@override.OriginalExtensions);
    }

    [Fact]
    public void OccurrenceOverride_WhenCreatedWithEmptyOrganization_ShouldBeValid()
    {
        // Arrange & Act
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Assert
        Assert.Empty(@override.Organization);
    }

    [Fact]
    public void OccurrenceOverride_WhenCreatedWithEmptyResourcePath_ShouldBeValid()
    {
        // Arrange & Act
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Assert
        Assert.Empty(@override.ResourcePath);
    }

    [Fact]
    public void OccurrenceOverride_Initialize_ShouldOverridePreviousValues()
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
        @override.Initialize(DateTime.UtcNow, TimeSpan.FromHours(1));

        // Act - Re-initialize with different values
        var newStartTime = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDuration = TimeSpan.FromMinutes(30);
        @override.Initialize(newStartTime, newDuration);

        // Assert
        Assert.Equal(newStartTime, @override.StartTime);
        Assert.Equal(newDuration, @override.Duration);
        Assert.Equal(newStartTime.Add(newDuration), @override.EndTime);
    }

    [Fact]
    public void OccurrenceOverride_OriginalTimeUtc_CanDifferFromStartTime()
    {
        // Arrange - This represents moving an occurrence to a different time
        var originalTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var newStartTime = new DateTime(2026, 1, 15, 14, 0, 0, DateTimeKind.Utc);

        // Act
        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = originalTime,
            OriginalDuration = TimeSpan.FromHours(1)
        };
        @override.Initialize(newStartTime, TimeSpan.FromHours(1));

        // Assert - The occurrence was "moved" from 10:00 to 14:00
        Assert.Equal(originalTime, @override.OriginalTimeUtc);
        Assert.Equal(newStartTime, @override.StartTime);
        Assert.NotEqual(@override.OriginalTimeUtc, @override.StartTime);
    }

    [Fact]
    public void OccurrenceOverride_WhenCreatedWithAllProperties_ShouldHaveCorrectValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var recurrenceId = Guid.NewGuid();
        var originalTime = new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        var newStartTime = new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var originalDuration = TimeSpan.FromHours(1);
        var newDuration = TimeSpan.FromMinutes(90);
        var originalExtensions = new Dictionary<string, string> { ["room"] = "A" };
        var newExtensions = new Dictionary<string, string> { ["room"] = "B" };

        // Act
        var @override = new OccurrenceOverride
        {
            Id = id,
            Organization = "company",
            ResourcePath = "meetings/team-a",
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = originalTime,
            OriginalDuration = originalDuration,
            OriginalExtensions = originalExtensions,
            Extensions = newExtensions
        };
        @override.Initialize(newStartTime, newDuration);

        // Assert
        Assert.Equal(id, @override.Id);
        Assert.Equal("company", @override.Organization);
        Assert.Equal("meetings/team-a", @override.ResourcePath);
        Assert.Equal(recurrenceId, @override.RecurrenceId);
        Assert.Equal(originalTime, @override.OriginalTimeUtc);
        Assert.Equal(originalDuration, @override.OriginalDuration);
        Assert.Equivalent(originalExtensions, @override.OriginalExtensions);
        Assert.Equal(newStartTime, @override.StartTime);
        Assert.Equal(newDuration, @override.Duration);
        Assert.Equal(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc), @override.EndTime);
        Assert.Equivalent(newExtensions, @override.Extensions);
    }
}
