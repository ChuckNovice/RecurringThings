namespace RecurringThings.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using RecurringThings.Benchmarks.Infrastructure;
using RecurringThings.Engine;
using RecurringThings.Models;

/// <summary>
/// Benchmarks for query operations.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class QueryBenchmarks
{
    private IRecurrenceEngine _engine = null!;
    private List<CalendarEntry> _seededRecurrences = null!;

    /// <summary>
    /// Gets or sets the database provider to benchmark.
    /// </summary>
    [Params(BenchmarkProvider.MongoDB, BenchmarkProvider.PostgreSQL)]
    public BenchmarkProvider Provider { get; set; }

    /// <summary>
    /// Gets or sets the number of recurrences to seed.
    /// </summary>
    [Params(100, 1000, 10000)]
    public int RecurrenceCount { get; set; }

    private DateTime _queryStart;
    private DateTime _queryEnd;

    /// <summary>
    /// Setup that runs once before all benchmark iterations for each parameter combination.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        Console.WriteLine();
        Console.WriteLine($"[QueryBenchmarks] GlobalSetup: Provider={Provider}, RecurrenceCount={RecurrenceCount}");

        _engine = ProviderFactory.CreateEngine(Provider);
        _seededRecurrences = await DataSeeder.SeedRecurrencesAsync(_engine, RecurrenceCount);

        // Query range spans 3 months of virtualized occurrences
        _queryStart = DateTime.UtcNow.Date;
        _queryEnd = _queryStart.AddMonths(3);

        Console.WriteLine($"[QueryBenchmarks] Setup complete. Ready to benchmark.");
    }

    /// <summary>
    /// Benchmarks querying virtualized occurrences over a 3-month period.
    /// </summary>
    [Benchmark(Description = "Query virtualized occurrences (3 months)")]
    public async Task<int> GetOccurrencesAsync()
    {
        var count = 0;
        await foreach (var entry in _engine.GetOccurrencesAsync(
            DataSeeder.Organization, DataSeeder.ResourcePath, _queryStart, _queryEnd))
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Benchmarks querying recurrence patterns.
    /// </summary>
    [Benchmark(Description = "Query recurrence patterns")]
    public async Task<int> GetRecurrencesAsync()
    {
        var count = 0;
        await foreach (var entry in _engine.GetRecurrencesAsync(
            DataSeeder.Organization, DataSeeder.ResourcePath, _queryStart, _queryEnd))
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Benchmarks querying with type filter.
    /// </summary>
    [Benchmark(Description = "Query with type filter")]
    public async Task<int> GetOccurrencesWithTypeFilterAsync()
    {
        var count = 0;
        await foreach (var entry in _engine.GetOccurrencesAsync(
            DataSeeder.Organization, DataSeeder.ResourcePath, _queryStart, _queryEnd,
            types: ["meeting-type-0", "meeting-type-1"]))
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Cleanup that runs once after all benchmark iterations for each parameter combination.
    /// </summary>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        Console.WriteLine();
        Console.WriteLine($"[QueryBenchmarks] GlobalCleanup: Deleting {_seededRecurrences.Count} recurrences...");

        var deleted = 0;
        foreach (var entry in _seededRecurrences)
        {
            if (entry.RecurrenceId.HasValue)
            {
                await _engine.DeleteRecurrenceAsync(
                    DataSeeder.Organization, DataSeeder.ResourcePath, entry.RecurrenceId.Value);
                deleted++;
            }
        }

        Console.WriteLine($"[QueryBenchmarks] Cleanup complete: {deleted} recurrences deleted.");
    }
}
