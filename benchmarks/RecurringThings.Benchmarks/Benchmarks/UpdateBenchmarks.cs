namespace RecurringThings.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using RecurringThings.Benchmarks.Infrastructure;
using RecurringThings.Engine;
using RecurringThings.Models;

/// <summary>
/// Benchmarks for update operations.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class UpdateBenchmarks
{
    private IRecurrenceEngine _engine = null!;
    private List<CalendarEntry> _standaloneOccurrences = null!;
    private List<CalendarEntry> _virtualizedOccurrences = null!;
    private CalendarEntry? _recurrenceEntry;
    private int _updateIndex;

    /// <summary>
    /// Gets or sets the database provider to benchmark.
    /// </summary>
    [Params(BenchmarkProvider.MongoDB, BenchmarkProvider.PostgreSQL)]
    public BenchmarkProvider Provider { get; set; }

    /// <summary>
    /// Gets or sets the number of occurrences to seed.
    /// </summary>
    [Params(100)]
    public int OccurrenceCount { get; set; }

    private DateTime _queryStart;
    private DateTime _queryEnd;

    /// <summary>
    /// Setup that runs once before all benchmark iterations for each parameter combination.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        Console.WriteLine();
        Console.WriteLine($"[UpdateBenchmarks] GlobalSetup: Provider={Provider}, OccurrenceCount={OccurrenceCount}");

        _engine = ProviderFactory.CreateEngine(Provider);
        _updateIndex = 0;

        // Seed standalone occurrences
        _standaloneOccurrences = await DataSeeder.SeedOccurrencesAsync(_engine, OccurrenceCount);

        // Seed one recurrence and get virtualized occurrences
        var recurrences = await DataSeeder.SeedRecurrencesAsync(_engine, 1);
        _recurrenceEntry = recurrences.First();

        _queryStart = DateTime.UtcNow.Date;
        _queryEnd = _queryStart.AddMonths(1);

        Console.WriteLine("  Fetching virtualized occurrences for update tests...");
        _virtualizedOccurrences = new List<CalendarEntry>();
        await foreach (var entry in _engine.GetOccurrencesAsync(
            DataSeeder.Organization, DataSeeder.ResourcePath, _queryStart, _queryEnd))
        {
            if (entry.EntryType == CalendarEntryType.Virtualized)
            {
                _virtualizedOccurrences.Add(entry);
                if (_virtualizedOccurrences.Count >= 20)
                {
                    break;
                }
            }
        }

        Console.WriteLine($"[UpdateBenchmarks] Setup complete. {_virtualizedOccurrences.Count} virtualized occurrences ready.");
    }

    /// <summary>
    /// Benchmarks updating a standalone occurrence.
    /// </summary>
    [Benchmark(Description = "Update standalone occurrence")]
    public async Task<CalendarEntry> UpdateStandaloneOccurrenceAsync()
    {
        var index = Interlocked.Increment(ref _updateIndex) % _standaloneOccurrences.Count;
        var entry = _standaloneOccurrences[index];
        entry.Duration = TimeSpan.FromMinutes(60 + (index % 30));
        return await _engine.UpdateOccurrenceAsync(entry);
    }

    /// <summary>
    /// Benchmarks updating a virtualized occurrence (creates an override).
    /// </summary>
    [Benchmark(Description = "Update virtualized occurrence (creates override)")]
    public async Task<CalendarEntry> UpdateVirtualizedOccurrenceAsync()
    {
        var index = Interlocked.Increment(ref _updateIndex) % _virtualizedOccurrences.Count;
        var entry = _virtualizedOccurrences[index];
        entry.Duration = TimeSpan.FromMinutes(90);
        return await _engine.UpdateOccurrenceAsync(entry);
    }

    /// <summary>
    /// Cleanup that runs once after all benchmark iterations for each parameter combination.
    /// </summary>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        Console.WriteLine();
        Console.WriteLine($"[UpdateBenchmarks] GlobalCleanup: Deleting test data...");

        var deleted = 0;
        foreach (var entry in _standaloneOccurrences)
        {
            try
            {
                await _engine.DeleteOccurrenceAsync(entry);
                deleted++;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        if (_recurrenceEntry?.RecurrenceId.HasValue == true)
        {
            await _engine.DeleteRecurrenceAsync(
                DataSeeder.Organization, DataSeeder.ResourcePath, _recurrenceEntry.RecurrenceId.Value);
            deleted++;
        }

        Console.WriteLine($"[UpdateBenchmarks] Cleanup complete: {deleted} entries deleted.");
    }
}
