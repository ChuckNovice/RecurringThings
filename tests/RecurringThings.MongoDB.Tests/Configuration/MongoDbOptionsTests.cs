namespace RecurringThings.MongoDB.Tests.Configuration;

using System;
using RecurringThings.MongoDB.Configuration;
using Xunit;

public class MongoDbOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new MongoDbOptions();

        // Assert
        Assert.Equal(string.Empty, options.ConnectionString);
        Assert.Equal(string.Empty, options.DatabaseName);
        Assert.Equal("recurring_things", options.CollectionName);
        Assert.True(options.CreateIndexesOnStartup);
    }

    #endregion

    #region Validate - ConnectionString Tests

    [Fact]
    public void Validate_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = null!,
            DatabaseName = "test-db",
            CollectionName = "test-collection"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("ConnectionString", ex.ParamName);
        Assert.Contains("connection string is required", ex.Message);
    }

    [Fact]
    public void Validate_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = string.Empty,
            DatabaseName = "test-db",
            CollectionName = "test-collection"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("ConnectionString", ex.ParamName);
    }

    [Fact]
    public void Validate_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "   ",
            DatabaseName = "test-db",
            CollectionName = "test-collection"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("ConnectionString", ex.ParamName);
    }

    #endregion

    #region Validate - DatabaseName Tests

    [Fact]
    public void Validate_WithNullDatabaseName_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = null!,
            CollectionName = "test-collection"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("DatabaseName", ex.ParamName);
        Assert.Contains("database name is required", ex.Message);
    }

    [Fact]
    public void Validate_WithEmptyDatabaseName_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = string.Empty,
            CollectionName = "test-collection"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("DatabaseName", ex.ParamName);
    }

    [Fact]
    public void Validate_WithWhitespaceDatabaseName_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "   ",
            CollectionName = "test-collection"
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("DatabaseName", ex.ParamName);
    }

    #endregion

    #region Validate - CollectionName Tests

    [Fact]
    public void Validate_WithNullCollectionName_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "test-db",
            CollectionName = null!
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("CollectionName", ex.ParamName);
        Assert.Contains("collection name is required", ex.Message);
    }

    [Fact]
    public void Validate_WithEmptyCollectionName_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "test-db",
            CollectionName = string.Empty
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("CollectionName", ex.ParamName);
    }

    [Fact]
    public void Validate_WithWhitespaceCollectionName_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "test-db",
            CollectionName = "   "
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("CollectionName", ex.ParamName);
    }

    #endregion

    #region Validate - Success Tests

    [Fact]
    public void Validate_WithAllValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "test-db",
            CollectionName = "test-collection"
        };

        // Act & Assert - should not throw
        var exception = Record.Exception(options.Validate);
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithDefaultCollectionName_DoesNotThrow()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "test-db"
            // CollectionName uses default "recurring_things"
        };

        // Act & Assert - should not throw
        var exception = Record.Exception(options.Validate);
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithMongoDbSrvConnectionString_DoesNotThrow()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "mongodb+srv://user:pass@cluster.mongodb.net",
            DatabaseName = "test-db",
            CollectionName = "test-collection"
        };

        // Act & Assert - should not throw
        var exception = Record.Exception(options.Validate);
        Assert.Null(exception);
    }

    #endregion

    #region Validation Order Tests

    [Fact]
    public void Validate_WithMultipleInvalidOptions_ThrowsForConnectionStringFirst()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = string.Empty,
            DatabaseName = string.Empty,
            CollectionName = string.Empty
        };

        // Act & Assert - ConnectionString is validated first
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("ConnectionString", ex.ParamName);
    }

    [Fact]
    public void Validate_WithInvalidDatabaseAndCollection_ThrowsForDatabaseFirst()
    {
        // Arrange
        var options = new MongoDbOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = string.Empty,
            CollectionName = string.Empty
        };

        // Act & Assert - DatabaseName is validated after ConnectionString
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("DatabaseName", ex.ParamName);
    }

    #endregion
}
