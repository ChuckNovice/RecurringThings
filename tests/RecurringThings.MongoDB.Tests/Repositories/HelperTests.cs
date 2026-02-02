namespace RecurringThings.MongoDB.Tests.Repositories;

using global::MongoDB.Driver;
using Moq;
using RecurringThings.MongoDB.Repositories;
using Transactional.Abstractions;
using Transactional.MongoDB;

/// <summary>
/// Tests for the <see cref="Helper"/> class.
/// </summary>
public class HelperTests
{
    #region GetSession Tests

    [Fact]
    public void GetSession_WithNullContext_ReturnsNull()
    {
        // Arrange
        ITransactionContext? context = null;

        // Act
        var result = Helper.GetSession(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSession_WithValidMongoContext_ReturnsSession()
    {
        // Arrange
        var mockSession = new Mock<IClientSessionHandle>();
        var mockContext = new Mock<IMongoTransactionContext>();
        mockContext.Setup(c => c.Session).Returns(mockSession.Object);

        // Act
        var result = Helper.GetSession(mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockSession.Object, result);
    }

    [Fact]
    public void GetSession_WithInvalidContextType_ThrowsArgumentException()
    {
        // Arrange
        var mockContext = new Mock<ITransactionContext>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Helper.GetSession(mockContext.Object));
        Assert.Equal("transactionContext", ex.ParamName);
        Assert.Contains("IMongoTransactionContext", ex.Message);
        Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
