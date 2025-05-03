namespace RecurringThings.Tests;

using Core.Domain;
using Core.Repository;
using FakeDatabase;
using FakeDatabase.Repository;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Unit tests of the <see cref="RecurrenceRepository"/> class.
/// </summary>
[TestClass]
public class RecurrenceRepositoryTests
{

    private IServiceScope _scope;
    private IRecurrenceRepository _recurrenceRepository;

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
        _recurrenceRepository = _scope.ServiceProvider.GetRequiredService<IRecurrenceRepository>();
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
    public async Task InsertAsync_WhenRecurrenceIsInserted_ShouldBeRetrievableViaGetInRangeAsync()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 5, 5, 9, 0, 0, DateTimeKind.Utc),
            TimeZone = "UTC",
            RRule = "FREQ=DAILY",
            Description = "Sample recurrence"
        };

        var rangeStart = recurrence.StartTime;
        var rangeEnd = recurrence.RecurrenceEndTime.AddDays(1);

        // Act
        await _recurrenceRepository.InsertAsync(recurrence);
        var results = await _recurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == recurrence.Id);

        Assert.IsNotNull(retrieved, "Recurrence was not retrieved.");
        Assert.AreEqual(recurrence.StartTime, retrieved.StartTime);
        Assert.AreEqual(recurrence.Duration, retrieved.Duration);
        Assert.AreEqual(recurrence.RecurrenceEndTime, retrieved.RecurrenceEndTime);
        Assert.AreEqual(recurrence.TimeZone, retrieved.TimeZone);
        Assert.AreEqual(recurrence.RRule, retrieved.RRule);
        Assert.AreEqual(recurrence.Description, retrieved.Description);
    }

    [TestMethod]
    public async Task InsertAsync_WhenRecurrenceIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        Recurrence recurrence = null;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
        {
            await _recurrenceRepository.InsertAsync(recurrence);
        });
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenNoRecurrencesExist_ShouldReturnEmptyList()
    {
        // Arrange
        var start = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(1);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(start, end);

        // Assert
        Assert.IsNotNull(results, "Result should not be null.");
        Assert.AreEqual(0, results.Count, "Expected no recurrences to be returned.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenRecurrenceFullyInsideRange_ShouldBeReturned()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 5, 2, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 5, 4, 9, 0, 0, DateTimeKind.Utc),
            TimeZone = "UTC",
            RRule = "FREQ=DAILY",
            Description = "Inside range"
        };

        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == recurrence.Id);
        Assert.IsNotNull(retrieved, "Expected recurrence to be returned.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenRecurrenceStartsBeforeAndEndsInsideRange_ShouldBeReturned()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 4, 28, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 5, 2, 9, 0, 0, DateTimeKind.Utc),
            TimeZone = "UTC",
            RRule = "FREQ=DAILY",
            Description = "Starts before, ends inside"
        };

        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == recurrence.Id);
        Assert.IsNotNull(retrieved, "Expected recurrence to be returned.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenRecurrenceStartsInsideAndEndsAfterRange_ShouldBeReturned()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 5, 3, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 5, 7, 9, 0, 0, DateTimeKind.Utc),
            TimeZone = "UTC",
            RRule = "FREQ=DAILY",
            Description = "Starts inside, ends after"
        };

        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == recurrence.Id);
        Assert.IsNotNull(retrieved, "Expected recurrence to be returned.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenRecurrenceSpansEntireRange_ShouldBeReturned()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 4, 30, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 5, 6, 9, 0, 0, DateTimeKind.Utc),
            TimeZone = "UTC",
            RRule = "FREQ=DAILY",
            Description = "Spans entire range"
        };

        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == recurrence.Id);
        Assert.IsNotNull(retrieved, "Expected recurrence to be returned.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenRecurrenceEndsExactlyAtRangeStart_ShouldBeExcluded()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 4, 25, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            TimeZone = "UTC",
            RRule = "FREQ=DAILY",
            Description = "Ends at range start"
        };

        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == recurrence.Id);
        Assert.IsNull(retrieved, "Recurrence ending exactly at the range start should be excluded.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenRecurrenceStartsExactlyAtRangeEnd_ShouldBeExcluded()
    {
        // Arrange
        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = rangeEnd,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = rangeEnd.AddDays(3),
            TimeZone = "UTC",
            RRule = "FREQ=DAILY",
            Description = "Starts at range end"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == recurrence.Id);
        Assert.IsNull(retrieved, "Recurrence starting exactly at the range end should be excluded.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenRecurrenceIsFullyBeforeRange_ShouldBeExcluded()
    {
        // Arrange
        var rangeStart = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2025, 5, 10, 0, 0, 0, DateTimeKind.Utc);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 4, 25, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            TimeZone = "UTC",
            RRule = "FREQ=DAILY",
            Description = "Fully before range"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == recurrence.Id);
        Assert.IsNull(retrieved, "Recurrence fully before the range should be excluded.");
    }

    [TestMethod]
    public async Task GetInRangeAsync_WhenRecurrenceIsFullyAfterRange_ShouldBeExcluded()
    {
        // Arrange
        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 5, 6, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 5, 10, 9, 0, 0, DateTimeKind.Utc),
            TimeZone = "UTC",
            RRule = "FREQ=DAILY",
            Description = "Fully after range"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        var retrieved = results.SingleOrDefault(x => x.Id == recurrence.Id);
        Assert.IsNull(retrieved, "Recurrence fully after the range should be excluded.");
    }

    [TestMethod]
    public async Task InsertAsync_WhenMultipleRecurrencesInserted_ShouldPersistAndReturnIndependently()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 8, 0, 0, DateTimeKind.Utc);

        var recurrences = Enumerable.Range(0, 3).Select(i =>
            new Recurrence
            {
                Id = Guid.NewGuid(),
                StartTime = baseTime.AddDays(i),
                Duration = TimeSpan.FromHours(1),
                RecurrenceEndTime = baseTime.AddDays(i + 2),
                TimeZone = "UTC",
                RRule = "FREQ=DAILY",
                Description = $"Recurrence {i}"
            }).ToList();

        foreach (var recurrence in recurrences)
            await _recurrenceRepository.InsertAsync(recurrence);

        var rangeStart = baseTime;
        var rangeEnd = baseTime.AddDays(10);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(rangeStart, rangeEnd);

        // Assert
        foreach (var expected in recurrences)
        {
            var actual = results.SingleOrDefault(x => x.Id == expected.Id);
            Assert.IsNotNull(actual, $"Recurrence {expected.Description} was not found.");
            Assert.AreEqual(expected.StartTime, actual.StartTime);
            Assert.AreEqual(expected.Duration, actual.Duration);
            Assert.AreEqual(expected.RecurrenceEndTime, actual.RecurrenceEndTime);
            Assert.AreEqual(expected.Description, actual.Description);
        }
    }

    [TestMethod]
    public async Task InsertAsync_WhenRecurrenceHasExceptionsAndOverrides_ShouldPersistThem()
    {
        // Arrange
        var recurrenceId = Guid.NewGuid();
        var startTime = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Utc);

        var recurrence = new Recurrence
        {
            Id = recurrenceId,
            StartTime = startTime,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = startTime.AddDays(5),
            TimeZone = "UTC",
            RRule = "FREQ=DAILY",
            Description = "With exceptions and overrides",
        };

        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = startTime.AddDays(2),
            Recurrence = recurrence
        };

        var @override = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            OriginalTime = startTime.AddDays(1),
            StartTime = startTime.AddDays(1).AddHours(1),
            Duration = TimeSpan.FromHours(1),
            Description = "Overridden",
            Recurrence = recurrence
        };

        recurrence.Exceptions.Add(exception);
        recurrence.Overrides.Add(@override);

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _recurrenceRepository.GetInRangeAsync(startTime, startTime.AddDays(7));
        var retrieved = results.SingleOrDefault(r => r.Id == recurrenceId);

        // Assert
        Assert.IsNotNull(retrieved, "Expected recurrence to be returned.");
        Assert.AreEqual(1, retrieved.Exceptions.Count, "Expected one exception.");
        Assert.AreEqual(1, retrieved.Overrides.Count, "Expected one override.");

        var retrievedException = retrieved.Exceptions.Single();
        Assert.AreEqual(exception.OriginalTime, retrievedException.OriginalTime);

        var retrievedOverride = retrieved.Overrides.Single();
        Assert.AreEqual(@override.OriginalTime, retrievedOverride.OriginalTime);
        Assert.AreEqual(@override.StartTime, retrievedOverride.StartTime);
        Assert.AreEqual(@override.Duration, retrievedOverride.Duration);
        Assert.AreEqual(@override.Description, retrievedOverride.Description);
    }


}
