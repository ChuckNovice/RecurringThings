namespace RecurringThings.Tests.Domain;

using System;
using System.Collections.Generic;
using FluentAssertions;
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
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = originalTime
        };

        // Assert
        exception.Id.Should().Be(id);
        exception.Organization.Should().Be("org1");
        exception.ResourcePath.Should().Be("user/calendar");
        exception.RecurrenceId.Should().Be(recurrenceId);
        exception.OriginalTimeUtc.Should().Be(originalTime);
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
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Assert
        exception.Extensions.Should().BeNull();
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
        exception.Extensions.Should().HaveCount(2);
        exception.Extensions["reason"].Should().Be("Vacation");
        exception.Extensions["cancelledBy"].Should().Be("user123");
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
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Assert
        exception.Organization.Should().BeEmpty();
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
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Assert
        exception.ResourcePath.Should().BeEmpty();
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
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            Extensions = extensions
        };

        // Assert
        exception.Extensions.Should().BeEquivalentTo(extensions);
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
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow,
            Extensions = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Act
        exception.Extensions = null;

        // Assert
        exception.Extensions.Should().BeNull();
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
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Assert
        exception.Id.Should().Be(id);
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
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = DateTime.UtcNow
        };

        // Assert
        exception.RecurrenceId.Should().Be(recurrenceId);
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
            RecurrenceId = Guid.NewGuid(),
            OriginalTimeUtc = originalTime
        };

        // Assert
        exception.OriginalTimeUtc.Should().Be(originalTime);
    }
}
