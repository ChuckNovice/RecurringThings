namespace RecurringThings.Tests;

using Core;
using Core.Domain;
using Core.Repository;
using FakeDatabase;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Unit tests of the <see cref="VirtualizationService"/> class.
/// </summary>
[TestClass]
public class VirtualizationServiceTests
{

    private IServiceScope _scope;
    private IOccurrenceRepository _occurrenceRepository;
    private IRecurrenceRepository _recurrenceRepository;
    private IVirtualizationService _virtualizationService;

    /// <summary>
    ///     Initializes the test environment by configuring services.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services
            .AddScoped<IVirtualizationService, VirtualizationService>()
            .AddRepositories();

        _scope = services.BuildServiceProvider().CreateScope();
        _occurrenceRepository = _scope.ServiceProvider.GetRequiredService<IOccurrenceRepository>();
        _recurrenceRepository = _scope.ServiceProvider.GetRequiredService<IRecurrenceRepository>();
        _virtualizationService = _scope.ServiceProvider.GetRequiredService<IVirtualizationService>();
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
    public async Task GetOccurrencesAsync_WhenRecurrenceIsInRange_ShouldMaterializeOccurrences()
    {
        // Arrange
        var startTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        var recurrenceStart = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Utc);
        var recurrenceEndTime = new DateTime(2025, 5, 4, 9, 0, 0, DateTimeKind.Utc); // UNTIL is inclusive at 09:00 UTC

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = recurrenceStart,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = recurrenceEndTime,
            RRule = "FREQ=DAILY;UNTIL=20250504T090000Z",
            TimeZone = "UTC",
            Description = "Test recurrence"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(startTime, endTime);

        // Assert
        var occurrences = results
            .Where(x => x.Recurrence?.Id == recurrence.Id)
            .OrderBy(x => x.StartTime)
            .ToList();

        Assert.AreEqual(4, occurrences.Count, "Expected 4 materialized occurrences.");

        for (var i = 0; i < 4; i++)
        {
            var expectedStart = recurrenceStart.AddDays(i);
            var expectedDuration = TimeSpan.FromHours(1);

            Assert.AreEqual(expectedStart, occurrences[i].StartTime, $"StartTime mismatch on occurrence {i}");
            Assert.AreEqual(expectedDuration, occurrences[i].Duration, $"Duration mismatch on occurrence {i}");
            Assert.AreEqual(recurrence.Description, occurrences[i].Description, $"Description mismatch on occurrence {i}");
        }
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenRecurrenceIsOutOfRange_ShouldReturnEmpty()
    {
        // Arrange
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 4, 1, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 4, 05, 9, 0, 0, DateTimeKind.Utc),
            RRule = "FREQ=DAILY;UNTIL=20250405T090000Z",
            TimeZone = "UTC",
            Description = "Out of range recurrence"
        };

        var startTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(startTime, endTime);

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();
        Assert.AreEqual(0, occurrences.Count, "No occurrences should be returned for an out-of-range recurrence.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenRecurrenceStartsAtRangeStart_ShouldBeIncluded()
    {
        // Arrange
        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddDays(3);

        var recurrenceStart = rangeStart.AddHours(9); // 2025-05-01 09:00 UTC
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = recurrenceStart,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = rangeStart.AddDays(2).AddHours(9), // 2025-05-03 09:00 UTC
            RRule = "FREQ=DAILY;UNTIL=20250503T090000Z",
            TimeZone = "UTC",
            Description = "Starts at range start"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(rangeStart, rangeEnd);

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();

        Assert.IsTrue(occurrences.Any(), "Expected at least one materialized occurrence.");
        Assert.AreEqual(recurrenceStart, occurrences.First().StartTime, "First occurrence should start at range start.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenRecurrenceStartsAtRangeEnd_ShouldBeExcluded()
    {
        // Arrange
        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddDays(3);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = rangeEnd.AddHours(9), // Starts after range
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = rangeEnd.AddDays(2).AddHours(9),
            RRule = "FREQ=DAILY;UNTIL=20250405T090000Z",
            TimeZone = "UTC",
            Description = "Starts at range end"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(rangeStart, rangeEnd);

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();
        Assert.AreEqual(0, occurrences.Count, "No materialized occurrence should be returned for a recurrence starting at range end.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenExceptionMatchesOccurrence_ShouldRemoveIt()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(9),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(3).AddHours(9),
            RRule = "FREQ=DAILY;UNTIL=20250504T090000Z",
            TimeZone = "UTC",
            Description = "With exception"
        };

        recurrence.Exceptions.Add(new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = baseTime.AddDays(1).AddHours(9),
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, baseTime.AddDays(4));

        // Assert
        var removedTime = baseTime.AddDays(1).AddHours(9);
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();

        Assert.IsFalse(occurrences.Any(o => o.StartTime == removedTime), "Occurrence matching the exception should have been removed.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenExceptionDoesNotMatch_ShouldNotAffectOthers()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(9),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(3).AddHours(9),
            RRule = "FREQ=DAILY;UNTIL=20250504T090000Z",
            TimeZone = "UTC",
            Description = "With non-matching exception"
        };

        recurrence.Exceptions.Add(new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = baseTime.AddDays(10).AddHours(9), // Outside the range
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, baseTime.AddDays(4));

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();
        Assert.AreEqual(4, occurrences.Count, "All materialized occurrences should be present since exception is outside range.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenMultipleExceptionsExist_ShouldRemoveAllSpecified()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(9),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(4).AddHours(9),
            RRule = "FREQ=DAILY;UNTIL=20250505T090000Z",
            TimeZone = "UTC",
            Description = "With multiple exceptions"
        };

        recurrence.Exceptions.Add(new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = baseTime.AddDays(1).AddHours(9),
            Recurrence = recurrence
        });

        recurrence.Exceptions.Add(new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = baseTime.AddDays(2).AddHours(9),
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, baseTime.AddDays(5));

        // Assert
        var removedTimes = new[]
        {
            baseTime.AddDays(1).AddHours(9),
            baseTime.AddDays(2).AddHours(9)
        };

        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();

        foreach (var time in removedTimes)
        {
            Assert.IsFalse(occurrences.Any(x => x.StartTime == time), $"Occurrence at {time} should have been removed.");
        }

        Assert.AreEqual(3, occurrences.Count, "Three occurrences should remain after removing two.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenExceptionAtRangeStart_ShouldRemoveMatchingOccurrence()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(9),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(2).AddHours(9),
            RRule = "FREQ=DAILY;UNTIL=20250503T090000Z",
            TimeZone = "UTC",
            Description = "Exception at range start"
        };

        recurrence.Exceptions.Add(new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = baseTime.AddHours(9),
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, baseTime.AddDays(3));

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();

        Assert.IsFalse(occurrences.Any(x => x.StartTime == baseTime.AddHours(9)), "Occurrence at range start should have been removed.");
        Assert.AreEqual(2, occurrences.Count, "Expected two remaining occurrences after exception.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenAllOccurrencesAreExcluded_ShouldReturnNothing()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(9),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(2).AddHours(9),
            RRule = "FREQ=DAILY;UNTIL=20250503T090000Z",
            TimeZone = "UTC",
            Description = "Fully excluded recurrence"
        };

        recurrence.Exceptions.Add(new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = baseTime.AddHours(9),
            Recurrence = recurrence
        });
        recurrence.Exceptions.Add(new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = baseTime.AddDays(1).AddHours(9),
            Recurrence = recurrence
        });
        recurrence.Exceptions.Add(new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = baseTime.AddDays(2).AddHours(9),
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, baseTime.AddDays(4));

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();
        Assert.AreEqual(0, occurrences.Count, "All occurrences should be removed by exceptions.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenOverrideMatchesOccurrence_ShouldReplaceIt()
    {
        // Arrange
        var utcBase = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = utcBase.AddHours(9),               // 2025-05-01 09:00 UTC
            Duration = TimeSpan.FromHours(1),         
            RecurrenceEndTime = utcBase.AddDays(2).AddHours(9), // 2025-05-03 09:00 UTC
            RRule = "FREQ=DAILY;UNTIL=20250503T090000Z",
            TimeZone = "UTC",
            Description = "With override"
        };

        // Override the second occurrence (original: 2025-05-02 09:00 UTC)
        recurrence.Overrides.Add(new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            OriginalTime = utcBase.AddDays(1).AddHours(9),       // Original time
            StartTime = utcBase.AddDays(1).AddHours(15),         // New time: 2025-05-02 15:00 UTC
            Duration = TimeSpan.FromHours(1),
            Description = "Replaced",
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(utcBase, utcBase.AddDays(3));

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();
        Assert.AreEqual(3, occurrences.Count, "Expected 3 total occurrences: 2 from RRule, 1 override replaces 1.");

        var replaced = occurrences.SingleOrDefault(x => x.StartTime == utcBase.AddDays(1).AddHours(15));
        Assert.IsNotNull(replaced, "Override occurrence should exist at new time.");
        Assert.AreEqual("Replaced", replaced.Description);
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenOverrideProducesTimeOutsideRange_ShouldBeExcluded()
    {
        // Arrange
        var utcBase = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = utcBase.AddHours(9),                    // 2025-05-01 09:00 UTC
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = utcBase.AddDays(2).AddHours(9), // 2025-05-03 09:00 UTC
            RRule = "FREQ=DAILY;UNTIL=20250503T090000Z",
            TimeZone = "UTC",
            Description = "Override outside range"
        };

        // Override for 2025-05-02 moved outside search range
        recurrence.Overrides.Add(new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            OriginalTime = utcBase.AddDays(1).AddHours(9),       // Original occurrence
            StartTime = utcBase.AddDays(10).AddHours(9),         // Outside query range
            Duration = TimeSpan.FromHours(1),
            Description = "Shifted out",
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act — query covers 2025-05-01 to 2025-05-04 exclusively
        var results = await _virtualizationService.GetOccurrencesAsync(utcBase, utcBase.AddDays(3));

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();

        Assert.IsFalse(occurrences.Any(x => x.Description == "Shifted out"),
            "Override moved outside the range should not be returned.");

        Assert.IsTrue(occurrences.Any(x => x.StartTime == utcBase.AddHours(9)), "First occurrence should be present.");
        Assert.IsTrue(occurrences.Any(x => x.StartTime == utcBase.AddDays(2).AddHours(9)), "Third occurrence should be present.");
        Assert.AreEqual(2, occurrences.Count, "Expected two in-range occurrences.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenOverrideProducesTimeInsideRange_ShouldBeIncluded()
    {
        // Arrange
        var utcBase = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = utcBase.AddHours(9),                    // 2025-05-01 09:00 UTC
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = utcBase.AddDays(3).AddHours(9), // 2025-05-04 09:00 UTC
            RRule = "FREQ=DAILY;UNTIL=20250504T090000Z",
            TimeZone = "UTC",
            Description = "In-range override"
        };

        recurrence.Overrides.Add(new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            OriginalTime = utcBase.AddDays(1).AddHours(9),      // 2025-05-02 09:00 UTC
            StartTime = utcBase.AddDays(1).AddHours(12),        // 2025-05-02 12:00 UTC
            Duration = TimeSpan.FromHours(1),
            Description = "In range override",
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(utcBase, utcBase.AddDays(3));

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();
        Assert.IsTrue(
            occurrences.Any(x => x.StartTime == utcBase.AddDays(1).AddHours(12)),
            "Override occurrence should be included within the search range."
        );
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenOverrideAtRangeBoundary_ShouldRespectInclusionRules()
    {
        // Range: [2025-05-01 00:00:00, 2025-05-04 00:00:00)
        var rangeStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2025, 5, 4, 0, 0, 0, DateTimeKind.Utc);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc),
            RRule = "FREQ=DAILY;UNTIL=20250505T000000Z",
            TimeZone = "UTC",
            Description = "Boundary override"
        };

        // This override produces a StartTime at exactly 2025-05-04 00:00:00 — should be excluded.
        recurrence.Overrides.Add(new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            OriginalTime = new DateTime(2025, 5, 3, 9, 0, 0, DateTimeKind.Utc), // valid series time
            StartTime = new DateTime(2025, 5, 4, 0, 0, 0, DateTimeKind.Utc),   // outside the range (boundary)
            Duration = TimeSpan.FromHours(1),
            Description = "Out-of-range override",
            Recurrence = recurrence
        });

        // This override is comfortably inside the range and must be included.
        recurrence.Overrides.Add(new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            OriginalTime = new DateTime(2025, 5, 2, 9, 0, 0, DateTimeKind.Utc),
            StartTime = new DateTime(2025, 5, 2, 15, 0, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(1),
            Description = "In-range override",
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(rangeStart, rangeEnd);
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();

        // Assert
        Assert.IsTrue(
            occurrences.Any(x => x.Description == "In-range override"),
            "Expected in-range override to be included."
        );

        Assert.IsFalse(
            occurrences.Any(x => x.Description == "Out-of-range override"),
            "Expected boundary override to be excluded."
        );
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenOverrideAndExceptionShareOriginalTime_ShouldUseException()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var originalTime = baseTime.AddDays(1).AddHours(9);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(9),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(3).AddHours(9),
            RRule = "FREQ=DAILY;UNTIL=20250504T090000Z",
            TimeZone = "UTC",
            Description = "Override vs Exception"
        };

        // Both pointing to the same occurrence day
        recurrence.Exceptions.Add(new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = originalTime,
            Recurrence = recurrence
        });

        recurrence.Overrides.Add(new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            OriginalTime = originalTime,
            StartTime = originalTime.AddHours(2),
            Duration = TimeSpan.FromHours(1),
            Description = "Override wins (should be ignored)",
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, baseTime.AddDays(4));

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();

        Assert.IsFalse(occurrences.Any(x => x.StartTime == originalTime), "Original occurrence should not appear.");
        Assert.IsFalse(occurrences.Any(x => x.Description == "Override wins (should be ignored)"), "Override should be ignored when exception exists.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenOverrideOriginalTimeNotInSeries_ShouldBeIgnored()
    {
        // Arrange
        var utcBase = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = utcBase.AddHours(9),                    // 2025-05-01 09:00 UTC
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = utcBase.AddDays(2).AddHours(9), // 2025-05-03 09:00 UTC
            RRule = "FREQ=DAILY;UNTIL=20250503T090000Z",
            TimeZone = "UTC",
            Description = "Override invalid target"
        };

        recurrence.Overrides.Add(new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            OriginalTime = utcBase.AddDays(10).AddHours(9),     // 2025-05-11 09:00 UTC — outside the recurrence window
            StartTime = utcBase.AddDays(10).AddHours(15),
            Duration = TimeSpan.FromHours(1),
            Description = "Invalid override",
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(utcBase, utcBase.AddDays(4));

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();

        Assert.AreEqual(3, occurrences.Count, "Expected 3 normal daily occurrences.");
        Assert.IsFalse(occurrences.Any(x => x.Description == "Invalid override"), "Override outside series should be ignored.");
    }


    [TestMethod]
    public async Task GetOccurrencesAsync_WhenStandaloneOccurrenceIsInRange_ShouldBeIncluded()
    {
        // Arrange
        var startTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var endTime = startTime.AddDays(2);

        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = startTime.AddDays(1).AddHours(8),
            Duration = TimeSpan.FromHours(1),
            Description = "Standalone"
        };

        await _occurrenceRepository.InsertAsync(occurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(startTime, endTime);

        // Assert
        var match = results.SingleOrDefault(x => x.Id == occurrence.Id);
        Assert.IsNotNull(match, "Expected standalone occurrence to be included.");
        Assert.AreEqual("Standalone", match.Description);
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenStandaloneOccurrenceIsOutOfRange_ShouldBeExcluded()
    {
        // Arrange
        var startTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var endTime = startTime.AddDays(2);

        var occurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = startTime.AddDays(5).AddHours(8),
            Duration = TimeSpan.FromHours(1),
            Description = "Too late"
        };

        await _occurrenceRepository.InsertAsync(occurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(startTime, endTime);

        // Assert
        Assert.IsFalse(results.Any(x => x.Id == occurrence.Id), "Out-of-range standalone occurrence should be excluded.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenStandaloneOccurrenceAtRangeBoundaries_ShouldBehaveCorrectly()
    {
        // Arrange
        var startTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var endTime = startTime.AddDays(2);

        var inclusiveOccurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = startTime.AddHours(8),
            Duration = TimeSpan.FromHours(1),
            Description = "Inclusive"
        };

        var exclusiveOccurrence = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = endTime,
            Duration = TimeSpan.FromHours(1),
            Description = "Exclusive"
        };

        await _occurrenceRepository.InsertAsync(inclusiveOccurrence);
        await _occurrenceRepository.InsertAsync(exclusiveOccurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(startTime, endTime);

        // Assert
        Assert.IsTrue(results.Any(x => x.Id == inclusiveOccurrence.Id), "Inclusive occurrence should be returned.");
        Assert.IsFalse(results.Any(x => x.Id == exclusiveOccurrence.Id), "Exclusive occurrence at range end should be excluded.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenStandaloneAndVirtualizedOverlap_ShouldBothBeIncluded()
    {
        // Arrange
        var utcBase = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var utcEnd = utcBase.AddDays(2);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = utcBase.AddHours(9),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = utcBase.AddDays(3), // inclusive up to May 3
            RRule = "FREQ=DAILY;COUNT=3",
            TimeZone = "UTC",
            Description = "Daily"
        };

        var standalone = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = utcBase.AddHours(9), // overlaps with recurrence
            Duration = TimeSpan.FromHours(1),
            Description = "Standalone at same time"
        };

        await _recurrenceRepository.InsertAsync(recurrence);
        await _occurrenceRepository.InsertAsync(standalone);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(utcBase, utcEnd);

        // Assert
        Assert.IsTrue(results.Any(x => x.Id == standalone.Id), "Standalone occurrence should be included.");
        Assert.IsTrue(results.Any(x => x.Recurrence?.Id == recurrence.Id), "Recurrence occurrence should be included.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenTimeZoneIsApplied_ShouldAdjustMaterialization()
    {
        // Arrange
        const string tzId = "America/New_York";
        var tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        // Local time: 2025-05-01 09:00 in New York (EDT, UTC-4)
        var localStart = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Unspecified);
  
        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, tzInfo); // 2025-05-01T13:00Z
  
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = utcStart,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = utcStart,
            RRule = "FREQ=DAILY;COUNT=1",
            TimeZone = tzId,
            Description = "TZ Test"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        var queryStart = utcStart.Date;
        var queryEnd = queryStart.AddDays(1);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(queryStart, queryEnd);

        // Assert
        var occurrence = results.SingleOrDefault(x => x.Recurrence?.Id == recurrence.Id);
        Assert.IsNotNull(occurrence, "Expected one materialized occurrence.");
        Assert.AreEqual("TZ Test", occurrence.Description);
        Assert.AreEqual(utcStart, occurrence.StartTime, "StartTime should match expected UTC.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenTwoTimeZonesUsed_ShouldProduceDifferentResults()
    {
        // Arrange
        var utcStart = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var utcEnd = utcStart.AddDays(1);

        // Eastern Time (UTC-4 in May)
        var easternLocal = new DateTime(2025, 5, 1, 9, 0, 0);
        var easternTz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var easternUtc = TimeZoneInfo.ConvertTimeToUtc(easternLocal, easternTz);

        var recurrenceEastern = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = easternUtc,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = easternUtc,
            RRule = "FREQ=DAILY;COUNT=1",
            TimeZone = "America/New_York",
            Description = "Eastern"
        };

        // Pacific Time (UTC-7 in May)
        var pacificLocal = new DateTime(2025, 5, 1, 9, 0, 0);
        var pacificTz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        var pacificUtc = TimeZoneInfo.ConvertTimeToUtc(pacificLocal, pacificTz);

        var recurrencePacific = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = pacificUtc,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = pacificUtc,
            RRule = "FREQ=DAILY;COUNT=1",
            TimeZone = "America/Los_Angeles",
            Description = "Pacific"
        };

        await _recurrenceRepository.InsertAsync(recurrenceEastern);
        await _recurrenceRepository.InsertAsync(recurrencePacific);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(utcStart, utcEnd);

        // Assert
        var eastern = results.SingleOrDefault(x => x.Recurrence?.Id == recurrenceEastern.Id);
        var pacific = results.SingleOrDefault(x => x.Recurrence?.Id == recurrencePacific.Id);

        Assert.IsNotNull(eastern);
        Assert.IsNotNull(pacific);
        Assert.AreNotEqual(eastern.StartTime, pacific.StartTime,
            $"Expected different UTC times. Eastern={eastern.StartTime}, Pacific={pacific.StartTime}");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenOccurrenceFallsInSpringForwardGap_ShouldAdjustProperly()
    {
        // Arrange
        const string tzId = "America/New_York";
        var tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        var adjustedLocalTime = new DateTime(2025, 3, 9, 3, 0, 0);

        // Convert valid time to UTC
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(adjustedLocalTime, tzInfo);
        var endUtc = startUtc.AddHours(1);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = startUtc,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = endUtc,
            RRule = "FREQ=DAILY;COUNT=1",
            TimeZone = tzId,
            Description = "Spring Forward"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(startUtc, startUtc.AddHours(4));

        // Assert
        var occurrence = results.SingleOrDefault(x => x.Recurrence?.Id == recurrence.Id);
        Assert.IsNotNull(occurrence, "Occurrence during spring forward should still materialize.");
        Assert.AreEqual("Spring Forward", occurrence.Description);
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenTimeZoneApplied_ResultShouldReflectCorrectUtc()
    {
        // Arrange
        var tzId = "America/New_York";
        var tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var localStart = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Unspecified);
        var expectedUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tzInfo);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = expectedUtc,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = expectedUtc,
            RRule = "FREQ=DAILY;COUNT=1",
            TimeZone = tzId,
            Description = "UTC Mapping"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        var rangeStart = expectedUtc.Date;
        var rangeEnd = rangeStart.AddDays(1);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(rangeStart, rangeEnd);

        // Assert
        var occurrence = results.SingleOrDefault(x => x.Recurrence?.Id == recurrence.Id);
        Assert.IsNotNull(occurrence, "Expected materialized occurrence.");
        Assert.AreEqual(expectedUtc, occurrence.StartTime, "Occurrence should reflect correct UTC from local zone.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenRRULEExceedsRecurrenceEndTime_ShouldBeClipped()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Utc);
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(2), // 2025-05-03 09:00 UTC, inclusive
            RRule = "FREQ=DAILY;COUNT=10",
            TimeZone = "UTC",
            Description = "Clipped rule"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, baseTime.AddDays(10));

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();
        Assert.AreEqual(3, occurrences.Count, "Expected 3 occurrences: May 1, May 2, and May 3 (inclusive).");

        var expectedDates = new[]
        {
            new DateTime(2025, 5, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 5, 2, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 5, 3, 9, 0, 0, DateTimeKind.Utc),
        };

        foreach (var date in expectedDates)
        {
            Assert.IsTrue(occurrences.Any(o => o.StartTime == date), $"Missing expected occurrence on {date:yyyy-MM-dd}");
        }
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenValidRRULEYieldsNoDatesInRange_ShouldReturnEmpty()
    {
        // Arrange
        var ruleStart = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = ruleStart,
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = ruleStart.AddDays(5),
            RRule = "FREQ=DAILY;COUNT=5;UNTIL=20251102T013000Z",
            TimeZone = "UTC",
            Description = "No match in range"
        };

        var rangeStart = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = rangeStart.AddDays(2);

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(rangeStart, rangeEnd);

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();
        Assert.AreEqual(0, occurrences.Count, "Expected no matches from RRULE outside range.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenDenseRRULEUsed_ShouldNotCrashOrOverflow()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(9),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(1),
            RRule = "FREQ=MINUTELY;INTERVAL=1;UNTIL=20250502T000000Z",
            TimeZone = "UTC",
            Description = "High-frequency recurrence"
        };

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, baseTime.AddDays(1));

        // Assert
        var occurrences = results.Where(x => x.Recurrence?.Id == recurrence.Id).ToList();
        Assert.IsTrue(occurrences.Count > 0, "Expected dense recurrence to produce materialized occurrences.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenRecurrenceAndStandaloneExist_ShouldMergeCorrectly()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = baseTime.AddDays(2);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(10),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(2),
            RRule = "FREQ=DAILY;UNTIL=20251102T013000Z",
            TimeZone = "UTC",
            Description = "Series"
        };

        var standalone = new Occurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddDays(1).AddHours(15),
            Duration = TimeSpan.FromHours(1),
            Description = "Single"
        };

        await _recurrenceRepository.InsertAsync(recurrence);
        await _occurrenceRepository.InsertAsync(standalone);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, rangeEnd);

        // Assert
        Assert.IsTrue(results.Any(x => x.Recurrence?.Id == recurrence.Id), "Recurrence should produce virtualized occurrences.");
        Assert.IsTrue(results.Any(x => x.Id == standalone.Id), "Standalone occurrence should be included.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenExceptionsAndOverridesBothExist_ShouldApplyInCorrectOrder()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var targetTime = baseTime.AddDays(1).AddHours(9);

        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(9),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(3),
            RRule = "FREQ=DAILY;UNTIL=20250504T000000Z",
            TimeZone = "UTC",
            Description = "Complex rule"
        };

        recurrence.Exceptions.Add(new OccurrenceException
        {
            Id = Guid.NewGuid(),
            OriginalTime = targetTime,
            Recurrence = recurrence
        });

        recurrence.Overrides.Add(new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            OriginalTime = targetTime,
            StartTime = targetTime.AddHours(2),
            Duration = TimeSpan.FromHours(1),
            Description = "Override loses",
            Recurrence = recurrence
        });

        await _recurrenceRepository.InsertAsync(recurrence);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, baseTime.AddDays(4));

        // Assert
        Assert.IsFalse(results.Any(x => x.Description == "Override loses"), "Override should be ignored due to exception.");
        Assert.IsFalse(results.Any(x => x.StartTime == targetTime), "No occurrence should exist at the target time.");
    }

    [TestMethod]
    public async Task GetOccurrencesAsync_WhenMultipleRecurrencesExist_ShouldBeHandledIndependently()
    {
        // Arrange
        var baseTime = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = baseTime.AddDays(3);

        var recurrenceA = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(9),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(3),
            RRule = "FREQ=DAILY;UNTIL=20251102T013000Z",
            TimeZone = "UTC",
            Description = "A"
        };

        var recurrenceB = new Recurrence
        {
            Id = Guid.NewGuid(),
            StartTime = baseTime.AddHours(13),
            Duration = TimeSpan.FromHours(1),
            RecurrenceEndTime = baseTime.AddDays(3),
            RRule = "FREQ=DAILY;UNTIL=20251102T013000Z",
            TimeZone = "UTC",
            Description = "B"
        };

        await _recurrenceRepository.InsertAsync(recurrenceA);
        await _recurrenceRepository.InsertAsync(recurrenceB);

        // Act
        var results = await _virtualizationService.GetOccurrencesAsync(baseTime, rangeEnd);

        // Assert
        var countA = results.Count(x => x.Recurrence?.Id == recurrenceA.Id);
        var countB = results.Count(x => x.Recurrence?.Id == recurrenceB.Id);

        Assert.AreEqual(3, countA, "Recurrence A should produce 3 occurrences.");
        Assert.AreEqual(3, countB, "Recurrence B should produce 3 occurrences.");
    }


}