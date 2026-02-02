namespace RecurringThings.Tests.Engine;

using Moq;
using RecurringThings.Engine;
using RecurringThings.Exceptions;
using RecurringThings.Repository;

/// <summary>
/// Unit tests for <see cref="RecurrenceEngine.DeleteEventAsync"/>.
/// </summary>
public sealed class RecurrenceEngineDeleteEventAsyncTests
{
    private readonly Mock<IRecurringThingsRepository> _repositoryMock;
    private readonly RecurrenceEngine _engine;

    public RecurrenceEngineDeleteEventAsyncTests()
    {
        _repositoryMock = new Mock<IRecurringThingsRepository>();
        _repositoryMock
            .Setup(r => r.DeleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _engine = new RecurrenceEngine(_repositoryMock.Object);
    }

    #region Parameter Validation Tests

    /// <summary>
    /// Tests that DeleteEventAsync throws ArgumentNullException when uid is null.
    /// </summary>
    [Fact]
    public async Task GivenNullUid_WhenDeleteEventAsync_ThenThrowsArgumentNullException()
    {
        // Arrange
        string uid = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _engine.DeleteEventAsync(uid, "tenant1"));
    }

    /// <summary>
    /// Tests that DeleteEventAsync throws ArgumentException when uid is empty.
    /// </summary>
    [Fact]
    public async Task GivenEmptyUid_WhenDeleteEventAsync_ThenThrowsArgumentException()
    {
        // Arrange
        var uid = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _engine.DeleteEventAsync(uid, "tenant1"));
    }

    /// <summary>
    /// Tests that DeleteEventAsync throws ArgumentException when uid is whitespace.
    /// </summary>
    [Fact]
    public async Task GivenWhitespaceUid_WhenDeleteEventAsync_ThenThrowsArgumentException()
    {
        // Arrange
        var uid = "   ";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _engine.DeleteEventAsync(uid, "tenant1"));
    }

    #endregion

    #region Repository Call Verification Tests

    /// <summary>
    /// Tests that DeleteEventAsync calls repository with correct parameters.
    /// </summary>
    [Fact]
    public async Task GivenValidUid_WhenDeleteEventAsync_ThenCallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var uid = "test-uid";
        var tenantId = "my-tenant";
        var userId = "user-123";

        // Act
        await _engine.DeleteEventAsync(uid, tenantId, userId);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(
            uid,
            tenantId,
            userId,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that DeleteEventAsync passes null userId when not provided.
    /// </summary>
    [Fact]
    public async Task GivenNoUserId_WhenDeleteEventAsync_ThenPassesNullUserId()
    {
        // Arrange
        var uid = "test-uid";

        // Act
        await _engine.DeleteEventAsync(uid, "tenant1");

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(
            uid,
            "tenant1",
            null,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that DeleteEventAsync passes CancellationToken to repository.
    /// </summary>
    [Fact]
    public async Task GivenCancellationToken_WhenDeleteEventAsync_ThenPassesToRepository()
    {
        // Arrange
        var uid = "test-uid";
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _engine.DeleteEventAsync(uid, "tenant1", cancellationToken: token);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(
            uid,
            "tenant1",
            null,
            token),
            Times.Once);
    }

    #endregion

    #region Not Found Tests

    /// <summary>
    /// Tests that DeleteEventAsync throws EventNotFoundException when repository returns false.
    /// </summary>
    [Fact]
    public async Task GivenNonExistingEvent_WhenDeleteEventAsync_ThenThrowsEventNotFoundException()
    {
        // Arrange
        var uid = "non-existing-uid";
        var tenantId = "tenant1";
        var userId = "user1";

        _repositoryMock
            .Setup(r => r.DeleteAsync(uid, tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(
            () => _engine.DeleteEventAsync(uid, tenantId, userId));

        Assert.Equal(uid, exception.Uid);
        Assert.Equal(tenantId, exception.TenantId);
        Assert.Equal(userId, exception.UserId);
    }

    /// <summary>
    /// Tests that DeleteEventAsync throws EventNotFoundException with null userId when not found.
    /// </summary>
    [Fact]
    public async Task GivenNonExistingEventWithNullUserId_WhenDeleteEventAsync_ThenThrowsEventNotFoundExceptionWithNullUserId()
    {
        // Arrange
        var uid = "non-existing-uid";
        var tenantId = "tenant1";

        _repositoryMock
            .Setup(r => r.DeleteAsync(uid, tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(
            () => _engine.DeleteEventAsync(uid, tenantId));

        Assert.Equal(uid, exception.Uid);
        Assert.Equal(tenantId, exception.TenantId);
        Assert.Null(exception.UserId);
    }

    #endregion

    #region Success Tests

    /// <summary>
    /// Tests that DeleteEventAsync completes successfully when repository returns true.
    /// </summary>
    [Fact]
    public async Task GivenExistingEvent_WhenDeleteEventAsync_ThenCompletesSuccessfully()
    {
        // Arrange
        var uid = "existing-uid";
        var tenantId = "tenant1";
        var userId = "user1";

        _repositoryMock
            .Setup(r => r.DeleteAsync(uid, tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert - should not throw
        await _engine.DeleteEventAsync(uid, tenantId, userId);

        _repositoryMock.Verify(r => r.DeleteAsync(
            uid,
            tenantId,
            userId,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
