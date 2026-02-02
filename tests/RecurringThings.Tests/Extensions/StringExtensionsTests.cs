namespace RecurringThings.Tests.Extensions;

using RecurringThings.Extensions;

/// <summary>
/// Unit tests for <see cref="StringExtensions"/>.
/// </summary>
public sealed class StringExtensionsTests
{
    /// <summary>
    /// Tests that HasDuplicate returns false for an empty collection.
    /// </summary>
    [Fact]
    public void GivenEmptyCollection_WhenHasDuplicate_ThenReturnsFalse()
    {
        // Arrange
        var source = Array.Empty<string>();

        // Act
        var result = source.HasDuplicate(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that HasDuplicate returns false for a single item collection.
    /// </summary>
    [Fact]
    public void GivenSingleItem_WhenHasDuplicate_ThenReturnsFalse()
    {
        // Arrange
        var source = new[] { "item" };

        // Act
        var result = source.HasDuplicate(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that HasDuplicate returns false when all items are unique.
    /// </summary>
    [Fact]
    public void GivenUniqueItems_WhenHasDuplicate_ThenReturnsFalse()
    {
        // Arrange
        var source = new[] { "apple", "banana", "cherry" };

        // Act
        var result = source.HasDuplicate(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that HasDuplicate returns true when exact duplicates exist.
    /// </summary>
    [Fact]
    public void GivenExactDuplicates_WhenHasDuplicate_ThenReturnsTrue()
    {
        // Arrange
        var source = new[] { "apple", "banana", "apple" };

        // Act
        var result = source.HasDuplicate(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that HasDuplicate with OrdinalIgnoreCase returns true for case-different duplicates.
    /// </summary>
    [Fact]
    public void GivenCaseDifferentDuplicates_WhenHasDuplicateWithIgnoreCase_ThenReturnsTrue()
    {
        // Arrange
        var source = new[] { "Apple", "banana", "APPLE" };

        // Act
        var result = source.HasDuplicate(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that HasDuplicate with Ordinal returns false for case-different strings.
    /// </summary>
    [Fact]
    public void GivenCaseDifferentStrings_WhenHasDuplicateWithOrdinal_ThenReturnsFalse()
    {
        // Arrange
        var source = new[] { "Apple", "APPLE", "apple" };

        // Act
        var result = source.HasDuplicate(StringComparer.Ordinal);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that HasDuplicate with Ordinal returns true for exact case duplicates.
    /// </summary>
    [Fact]
    public void GivenExactCaseDuplicates_WhenHasDuplicateWithOrdinal_ThenReturnsTrue()
    {
        // Arrange
        var source = new[] { "Apple", "Banana", "Apple" };

        // Act
        var result = source.HasDuplicate(StringComparer.Ordinal);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that HasDuplicate throws ArgumentNullException when source is null.
    /// </summary>
    [Fact]
    public void GivenNullSource_WhenHasDuplicate_ThenThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<string> source = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => source.HasDuplicate(StringComparer.OrdinalIgnoreCase));
        Assert.Equal("source", exception.ParamName);
    }

    /// <summary>
    /// Tests that HasDuplicate throws ArgumentNullException when comparer is null.
    /// </summary>
    [Fact]
    public void GivenNullComparer_WhenHasDuplicate_ThenThrowsArgumentNullException()
    {
        // Arrange
        var source = new[] { "item" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => source.HasDuplicate(null!));
        Assert.Equal("comparer", exception.ParamName);
    }

    /// <summary>
    /// Tests that HasDuplicate detects duplicate at the beginning.
    /// </summary>
    [Fact]
    public void GivenDuplicateAtBeginning_WhenHasDuplicate_ThenReturnsTrue()
    {
        // Arrange
        var source = new[] { "apple", "apple", "banana", "cherry" };

        // Act
        var result = source.HasDuplicate(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that HasDuplicate detects duplicate at the end.
    /// </summary>
    [Fact]
    public void GivenDuplicateAtEnd_WhenHasDuplicate_ThenReturnsTrue()
    {
        // Arrange
        var source = new[] { "apple", "banana", "cherry", "cherry" };

        // Act
        var result = source.HasDuplicate(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that HasDuplicate works with IEnumerable (not just arrays).
    /// </summary>
    [Fact]
    public void GivenEnumerableWithDuplicates_WhenHasDuplicate_ThenReturnsTrue()
    {
        // Arrange
        IEnumerable<string> source = new List<string> { "a", "b", "c", "B" };

        // Act
        var result = source.HasDuplicate(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that HasDuplicate handles empty strings correctly.
    /// </summary>
    [Fact]
    public void GivenDuplicateEmptyStrings_WhenHasDuplicate_ThenReturnsTrue()
    {
        // Arrange
        var source = new[] { "", "item", "" };

        // Act
        var result = source.HasDuplicate(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that HasDuplicate handles whitespace strings correctly.
    /// </summary>
    [Fact]
    public void GivenDuplicateWhitespaceStrings_WhenHasDuplicate_ThenReturnsTrue()
    {
        // Arrange
        var source = new[] { " ", "item", " " };

        // Act
        var result = source.HasDuplicate(StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.True(result);
    }
}
