namespace RecurringThings.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using RecurringThings.Benchmarks.Infrastructure;
using RecurringThings.Engine;
using RecurringThings.Models;

/// <summary>
/// Benchmarks for create operations.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CreateBenchmarks
{
    private IRecurrenceEngine _engine = null!;
    private List<CalendarEntry> _createdEntries = null!;
    private int _createIndex;

    /// <summary>
    /// Gets or sets the database provider to benchmark.
    /// </summary>
    [Params(BenchmarkProvider.MongoDB, BenchmarkProvider.PostgreSQL)]
    public BenchmarkProvider Provider { get; set; }

    private const string TimeZone = "America/New_York";

    /// <summary>
    /// Setup that runs once before all benchmark iterations for each parameter combination.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        Console.WriteLine();
        Console.WriteLine($"[CreateBenchmarks] GlobalSetup: Provider={Provider}");

        _engine = ProviderFactory.CreateEngine(Provider);
        _createdEntries = new List<CalendarEntry>();
        _createIndex = 0;

        Console.WriteLine($"[CreateBenchmarks] Setup complete. Ready to benchmark.");
    }

    /// <summary>
    /// Benchmarks creating a recurrence pattern.
    /// </summary>
    [Benchmark(Description = "Create recurrence pattern")]
    public async Task<CalendarEntry> CreateRecurrenceAsync()
    {
        var index = Interlocked.Increment(ref _createIndex);
        var startTime = DateTime.UtcNow.Date.AddHours(9);
        var rrule = $"FREQ=DAILY;UNTIL={DateTime.UtcNow.AddMonths(6):yyyyMMdd}T235959Z";

        var entry = await _engine.CreateRecurrenceAsync(
            DataSeeder.Organization,
            DataSeeder.ResourcePath,
            $"benchmark-type-{index}",
            startTime,
            TimeSpan.FromHours(1),
            rrule,
            TimeZone);

        lock (_createdEntries)
        {
            _createdEntries.Add(entry);
        }

        return entry;
    }

    /// <summary>
    /// Benchmarks creating a standalone occurrence.
    /// </summary>
    [Benchmark(Description = "Create standalone occurrence")]
    public async Task<CalendarEntry> CreateOccurrenceAsync()
    {
        var index = Interlocked.Increment(ref _createIndex);
        var startTime = DateTime.UtcNow.Date.AddDays(index).AddHours(10);

        var entry = await _engine.CreateOccurrenceAsync(
            DataSeeder.Organization,
            DataSeeder.ResourcePath,
            $"appointment-{index}",
            startTime,
            TimeSpan.FromMinutes(45),
            TimeZone);

        lock (_createdEntries)
        {
            _createdEntries.Add(entry);
        }

        return entry;
    }

    /// <summary>
    /// Cleanup that runs once after all benchmark iterations for each parameter combination.
    /// </summary>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        Console.WriteLine();
        Console.WriteLine($"[CreateBenchmarks] GlobalCleanup: Deleting {_createdEntries.Count} entries...");

        var deleted = 0;
        foreach (var entry in _createdEntries)
        {
            try
            {
                if (entry.RecurrenceId.HasValue && entry.EntryType == CalendarEntryType.Recurrence)
                {
                    await _engine.DeleteRecurrenceAsync(
                        DataSeeder.Organization, DataSeeder.ResourcePath, entry.RecurrenceId.Value);
                    deleted++;
                }
                else if (entry.OccurrenceId.HasValue)
                {
                    await _engine.DeleteOccurrenceAsync(entry);
                    deleted++;
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        Console.WriteLine($"[CreateBenchmarks] Cleanup complete: {deleted} entries deleted.");
    }
}
