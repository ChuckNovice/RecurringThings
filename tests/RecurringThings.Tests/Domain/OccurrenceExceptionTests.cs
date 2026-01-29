namespace RecurringThings.Tests.Domain;

using System;
using System.Collections.Generic;
using RecurringThings.Domain;
using Xunit;

public class OccurrenceExceptionTests
{
    [Fact]
    public void OccurrenceException_WhenCreated_ShouldHaveAllRequiredProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var recurrenceId = Guid.NewGuid();
        var originalTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var exception = new OccurrenceException
        {
            Id = id,
            Organization = "org1",
            ResourcePath = "user/calendar",
            Type = "meeting",
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = originalTime
        };

        // Assert
        Assert.Equal(id, exception.Id);
        Assert.Equal("org1", exception.Organization);
        Assert.Equal("user/calendar", exception.ResourcePath);
        Assert.Equal(recurrenceId, exception.RecurrenceId);
        Assert.Equal(originalTime, exception.OriginalTimeUtc);
    }

    [Fact]
    public void OccurrenceException_Extensions_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Assert
        Assert.Null(exception.Extensions);
    }

    [Fact]
    public void OccurrenceException_Extensions_ShouldBeMutable()
    {
        // Arrange
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            Extensions = new Dictionary<string, string> { ["reason"] = "Holiday" }
        };

        // Act
        exception.Extensions = new Dictionary<string, string>
        {
            ["reason"] = "Vacation",
            ["cancelledBy"] = "user123"
        };

        // Assert
        Assert.Equal(2, exception.Extensions.Count);
        Assert.Equal("Vacation", exception.Extensions["reason"]);
        Assert.Equal("user123", exception.Extensions["cancelledBy"]);
    }

    [Fact]
    public void OccurrenceException_WhenCreatedWithEmptyOrganization_ShouldBeValid()
    {
        // Arrange & Act
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "",
            ResourcePath = "path",
            Type = "meeting",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Assert
        Assert.Empty(exception.Organization);
    }

    [Fact]
    public void OccurrenceException_WhenCreatedWithEmptyResourcePath_ShouldBeValid()
    {
        // Arrange & Act
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "",
            Type = "meeting",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Assert
        Assert.Empty(exception.ResourcePath);
    }

    [Fact]
    public void OccurrenceException_WhenCreatedWithExtensions_ShouldContainAllPairs()
    {
        // Arrange
        var extensions = new Dictionary<string, string>
        {
            ["reason"] = "Cancelled due to weather",
            ["notifiedUsers"] = "true",
            ["cancelledAt"] = "2026-01-15T08:30:00Z"
        };

        // Act
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            Extensions = extensions
        };

        // Assert
        Assert.Equivalent(extensions, exception.Extensions);
    }

    [Fact]
    public void OccurrenceException_Extensions_CanBeSetToNull()
    {
        // Arrange
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            Extensions = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Act
        exception.Extensions = null;

        // Assert
        Assert.Null(exception.Extensions);
    }

    [Fact]
    public void OccurrenceException_Id_ShouldBeInitOnly()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var exception = new OccurrenceException
        {
            Id = id,
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(id, exception.Id);
    }

    [Fact]
    public void OccurrenceException_RecurrenceId_ShouldBeInitOnly()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();

        // Act
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(recurrenceId, exception.RecurrenceId);
    }

    [Fact]
    public void OccurrenceException_OriginalTimeUtc_ShouldBeInitOnly()
    {
        // Arrange
        var originalTime = new DateTime(2026, 3, 15, 14, 0, 0, DateTimeKind.Utc);

        // Act
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = "org",
            ResourcePath = "path",
            Type = "meeting",
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = originalTime
        };

        // Assert
        Assert.Equal(originalTime, exception.OriginalTimeUtc);
    }
}
