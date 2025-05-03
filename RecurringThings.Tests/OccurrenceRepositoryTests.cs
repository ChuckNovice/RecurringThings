namespace RecurringThings.Tests;

using Core.Domain;
using Core.Repository;
using FakeDatabase;
using FakeDatabase.Repository;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Unit tests of the <see cref="OccurrenceRepository"/> class.
/// </summary>
[TestClass]
public class OccurrenceRepositoryTests
{

    private IServiceScope _scope;
    private IOccurrenceRepository _occurrenceRepository;

    /// <summary>
    ///     Initializes the test environment by configuring services.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services
            .AddRepositories();

        _scope = services.BuildServiceProvider().CreateScope();
        _occurrenceRepository = _scope.ServiceProvider.GetRequiredService<IOccurrenceRepository>();
    }

    /// <summary>
    ///     Clean up the resources used by the tests.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public async Task InsertAsync_WhenOccurrenceIsInserted_ShouldBeRetrievableViaGetInRangeAsync()
    {
        // Arrange
        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            Description = "Test occurrence"
        };

        var rangeStart = occurrence.StartTime.AddHours(-1);
        var rangeEnd = occurrence.StartTime.AddHours(1);

        // Act
        await _occurrenceRepository.InsertAsync(occurrence);
        var results = await _occurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == occurrence.Id);

        Assert.IsNotNull(retrieved, "Occurrence was not retrieved.");
        Assert.AreEqual(occurrence.StartTime, retrieved.StartTime, "StartTime mismatch.");
        Assert.AreEqual(occurrence.Duration, retrieved.Duration, "Duration mismatch.");
        Assert.AreEqual(occurrence.Description, retrieved.Description, "Description mismatch.");
    }

    [TestMethod]
    public async Task InsertAsync_WhenOccurrenceIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        Occurrence occurrence = null;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
        {
            await _occurrenceRepository.InsertAsync(occurrence);
        });
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenNoOccurrencesExist_ShouldReturnEmptyList()
    {
        // Arrange
        var start = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(1);

        // Act
        var results = await _occurrenceRepository.GetInRangeAsync(start, end);

        // Assert
        Assert.IsNotNull(results, "Result should not be null.");
        Assert.AreEqual(0, results.Count, "Expected no occurrences to be returned.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenOccurrencesAreWithinRange_ShouldReturnThem()
    {
        // Arrange
        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddDays(2);

        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = rangeStart.AddHours(5),
            Duration = TimeSpan.FromHours(1),
            Description = "Within range"
        };

        await _occurrenceRepository.InsertAsync(occurrence);

        // Act
        var results = await _occurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == occurrence.Id);

        Assert.IsNotNull(retrieved, "Expected occurrence to be returned.");
        Assert.AreEqual(occurrence.StartTime, retrieved.StartTime);
        Assert.AreEqual(occurrence.Duration, retrieved.Duration);
        Assert.AreEqual(occurrence.Description, retrieved.Description);
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenOccurrenceStartsExactlyAtRangeStart_ShouldBeIncluded()
    {
        // Arrange
        var rangeStart = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddHours(2);

        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = rangeStart,
            Duration = TimeSpan.FromHours(1),
            Description = "Starts at range start"
        };

        await _occurrenceRepository.InsertAsync(occurrence);

        // Act
        var results = await _occurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == occurrence.Id);

        Assert.IsNotNull(retrieved, "Occurrence starting at the range start should be included.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenOccurrenceStartsExactlyAtRangeEnd_ShouldBeExcluded()
    {
        // Arrange
        var rangeStart = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddHours(2);

        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = rangeEnd,
            Duration = TimeSpan.FromHours(1),
            Description = "Starts at range end"
        };

        await _occurrenceRepository.InsertAsync(occurrence);

        // Act
        var results = await _occurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == occurrence.Id);

        Assert.IsNull(retrieved, "Occurrence starting at the range end should be excluded.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenOccurrencesAreOutsideRange_ShouldNotReturnThem()
    {
        // Arrange
        var rangeStart = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddHours(2);

        var before = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = rangeStart.AddHours(-1),
            Duration = TimeSpan.FromHours(1),
            Description = "Before range"
        };

        var after = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = rangeEnd.AddMinutes(1),
            Duration = TimeSpan.FromHours(1),
            Description = "After range"
        };

        await _occurrenceRepository.InsertAsync(before);
        await _occurrenceRepository.InsertAsync(after);

        // Act
        var results = await _occurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        Assert.IsFalse(results.Any(x => x.Id == before.Id), "Occurrence before range should be excluded.");
        Assert.IsFalse(results.Any(x => x.Id == after.Id), "Occurrence after range should be excluded.");
    }

    [TestMethod]
    public async Task InsertAsync_WhenMultipleOccurrencesInserted_ShouldPersistAndReturnIndependently()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 8, 0, 0, DateTimeKind.Utc);

        var occurrences = Enumerable.Range(0, 3).Select(i =>
            new Occurrence
            {
                Id = Guid.NewGuid(),
                StartTime = baseTime.AddHours(i),
                Duration = TimeSpan.FromHours(1),
                Description = $"Occurrence {i}"
            }).ToList();

        foreach (var occurrence in occurrences)
            await _occurrenceRepository.InsertAsync(occurrence);

        var rangeStart = baseTime;
        var rangeEnd = baseTime.AddHours(5);

        // Act
        var results = await _occurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        foreach (var expected in occurrences)
        {
            var actual = results.SingleOrDefault(x => x.Id == expected.Id);
            Assert.IsNotNull(actual, $"Occurrence {expected.Description} was not found.");
            Assert.AreEqual(expected.StartTime, actual.StartTime);
            Assert.AreEqual(expected.Duration, actual.Duration);
            Assert.AreEqual(expected.Description, actual.Description);
        }
    }


}
