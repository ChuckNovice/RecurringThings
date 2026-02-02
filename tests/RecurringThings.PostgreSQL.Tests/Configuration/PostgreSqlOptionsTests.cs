namespace RecurringThings.PostgreSQL.Tests.Configuration;

using RecurringThings.PostgreSQL.Configuration;

/// <summary>
/// Tests for the <see cref="PostgreSqlOptions"/> class.
/// </summary>
public class PostgreSqlOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new PostgreSqlOptions();

        // Assert
        Assert.Equal(string.Empty, options.ConnectionString);
        Assert.True(options.RunMigrationsOnStartup);
        Assert.Null(options.ConfigureNpgsql);
    }

    #endregion

    #region Validate - ConnectionString Tests

    [Fact]
    public void Validate_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = null!
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
        var options = new PostgreSqlOptions
        {
            ConnectionString = string.Empty
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("ConnectionString", ex.ParamName);
    }

    [Fact]
    public void Validate_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = "   "
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("ConnectionString", ex.ParamName);
    }

    #endregion

    #region Validate - Success Tests

    [Fact]
    public void Validate_WithValidConnectionString_DoesNotThrow()
    {
        // Arrange
        var options = new PostgreSqlOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=user;Password=pass"
        };

        // Act & Assert - should not throw
        var exception = Record.Exception(options.Validate);
        Assert.Null(exception);
    }

    #endregion
}
